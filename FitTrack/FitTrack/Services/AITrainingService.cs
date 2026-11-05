using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using System.Text.Json;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Services;

public sealed record AiTrainingResult(string? Text, string? Error)
{
    public bool Success => !string.IsNullOrWhiteSpace(Text) && Error is null;
}

public interface IAITrainingService
{
    Task<AiTrainingResult> GetAdviceAsync(string userId, string? question, CancellationToken cancellationToken = default);
    Task<AiTrainingResult> GeneratePlanDraftAsync(string userId, string candidateExercisesJson, CancellationToken cancellationToken = default);
}

public sealed class AITrainingService(AppDbContext db, IHttpClientFactory clients, IConfiguration configuration, ILogger<AITrainingService> logger) : IAITrainingService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProviderGates = new(StringComparer.OrdinalIgnoreCase);
    private const string SafetyInstructions = "You are FitTrack's fitness coach." +
        " Be practical and concise. Give general training guidance, not medical advice." +
        " Do not diagnose. If pain or injury is reported, advise stopping the painful" +
        " movement and consulting a qualified health professional.";

    public Task<AiTrainingResult> GetAdviceAsync(string userId, string? question, CancellationToken cancellationToken = default) =>
        GetTextAsync(userId, SafetyInstructions, string.IsNullOrWhiteSpace(question)
            ? "Give one concise coaching recommendation for my next workout."
            : question.Trim(), cancellationToken);

    public Task<AiTrainingResult> GeneratePlanDraftAsync(string userId, string candidateExercisesJson, CancellationToken cancellationToken = default)
    {
        var instructions = SafetyInstructions + " Create a safe, realistic workout plan using only supplied exercise IDs. Return only valid JSON with this shape: {\"planName\":\"...\",\"days\":[{\"weekday\":\"Monday\",\"exercises\":[{\"exerciseId\":123,\"sets\":3,\"repetitions\":10,\"restSeconds\":60}]}]}. Include only preferred weekdays. Do not include markdown fences, comments, or invented IDs.";
        var request = "Generate a weekly plan from this exercise catalog. Each catalog item is already checked against the user's available equipment. Consider goal, experience, days and session duration. Choose a balanced mix, do not repeat exercises during the week, use modest volume for beginners and limit each training day to 3-6 exercises.\nEXERCISES JSON:\n" + candidateExercisesJson;
        return GetTextAsync(userId, instructions, request, cancellationToken);
    }

    private async Task<AiTrainingResult> GetTextAsync(string userId, string instructions, string request, CancellationToken cancellationToken)
    {
        var geminiKey = configuration["GEMINI_API_KEY"];
        var useGemini = !string.IsNullOrWhiteSpace(geminiKey);
        var provider = useGemini ? "Gemini" : "OpenAI";
        var key = useGemini ? geminiKey : configuration["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(key)) return new(null, "AI is not configured. Add GEMINI_API_KEY or OPENAI_API_KEY to your environment; the rest of FitTrack remains available.");

        var profile = await db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(item => item.IdentityUserId == userId, cancellationToken);
        if (profile is null || !profile.SetupCompleted) return new(null, "Complete your fitness profile before using the AI coach.");
        var activePlan = await db.WorkoutPlans.AsNoTracking().Include(plan => plan.PlanExercises)
            .FirstOrDefaultAsync(plan => plan.UserProfileId == profile.Id && plan.IsActive, cancellationToken);
        var recent = await db.WorkoutLogs.AsNoTracking().Where(log => log.UserProfileId == profile.Id)
            .OrderByDescending(log => log.CompletedAt).Take(8).ToListAsync(cancellationToken);
        var latestMeasurement = await db.ProgressEntries.AsNoTracking().Where(entry => entry.UserProfileId == profile.Id)
            .OrderByDescending(entry => entry.Date).FirstOrDefaultAsync(cancellationToken);
        var trainingDays = profile.PreferredTrainingDays.Count > 0 ? string.Join(", ", profile.PreferredTrainingDays) : $"{profile.TrainingDaysPerWeek} days per week";
        var context = $"PROFILE: goal={profile.Goal}; experience={profile.ExperienceLevel}; preferred days={trainingDays}; session duration={profile.SessionDurationMinutes} minutes; equipment={string.Join(", ", profile.Equipment)}.\n"
            + (activePlan is null ? "ACTIVE PLAN: none.\n" : $"ACTIVE PLAN: {activePlan.Goal}; exercises={string.Join(", ", activePlan.PlanExercises.Select(item => item.ExerciseName))}.\n")
            + $"RECENT LOGS: {(recent.Count == 0 ? "none" : string.Join("; ", recent.Select(item => $"{item.CompletedAt:yyyy-MM-dd} {item.ExerciseName} {item.SetsCompleted}x{item.RepetitionsCompleted} at {item.WeightUsed}kg")))}.\n"
            + (latestMeasurement is null ? "LATEST PROGRESS MEASUREMENT: none.\n" : $"LATEST MAX WEIGHT ENTRY: {latestMeasurement.MaxWeight}kg on {latestMeasurement.Date:yyyy-MM-dd}.\n")
            + "USER REQUEST:\n" + request;

        try
        {
            var client = clients.CreateClient(provider);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var model = useGemini
                ? configuration["GEMINI_MODEL"] ?? configuration["Gemini:Model"] ?? "gemini-3.8-flash"
                : configuration["OpenAI:Model"] ?? configuration["OPENAI_MODEL"] ?? "gpt-4o-mini";
            var path = useGemini ? "chat/completions" : "v1/responses";
            object body = useGemini
                ? new { model, messages = new[] { new { role = "system", content = instructions }, new { role = "user", content = context } } }
                : new { model, instructions, input = context };
            using var response = await SendQueuedWithRetryAsync(provider, client, path, body, useGemini, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var bodyText = await response.Content.ReadAsStringAsync(cancellationToken);
                var code = ReadErrorCode(bodyText);
                logger.LogWarning("{Provider} returned HTTP {StatusCode}, code {ErrorCode}.", provider, (int)response.StatusCode, code ?? "unknown");
                if (useGemini && response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    return new(null, "Gemini is temporarily busy or unavailable after retries. Try again later.");
                if (useGemini && response.StatusCode == HttpStatusCode.NotFound)
                    return new(null, $"Gemini could not find the configured model '{model}'. Check the model ID in appsettings.json or GEMINI_MODEL; for Flash-Lite, use 'gemini-3.5-flash-lite'.");
                if ((int)response.StatusCode == 429)
                {
                    if (useGemini)
                        return new(null, "Gemini usage or rate limit reached. Check AI Studio's quota and model access, then wait briefly before retrying.");

                    var normalizedCode = code?.ToLowerInvariant();
                    if (normalizedCode is "insufficient_quota" or "credit_balance_exhausted" or "organization_usage_limit_exceeded"
                        or "organization_spend_limit_exceeded" or "project_spend_limit_exceeded")
                        return new(null, "OpenAI has no available quota for this project. Check API billing or credit balance and the organization and project usage limits. ChatGPT subscription billing is separate.");

                    if (normalizedCode is "rate_limit_exceeded" or "requests" or "tokens")
                        return new(null, "OpenAI's temporary request or token rate limit was reached. Wait briefly before retrying and check this model's RPM and TPM limits if it continues.");

                    return new(null, "OpenAI returned a 429 usage limit response. Check API billing, credit balance, project usage limits, and rate limits. See the app output for the provider error code.");
                }
                return new(null, $"{provider} could not complete this request (HTTP {(int)response.StatusCode}). Saved plans and workout history are unaffected.");
            }
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var text = useGemini ? ReadChatCompletion(document.RootElement) : ReadResponsesText(document.RootElement);
            return string.IsNullOrWhiteSpace(text) ? new(null, $"{provider} returned an empty answer. Try again.") : new(text, null);
        }
        catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Provider} training request failed.", provider);
            return new(null, $"FitTrack could not reach {provider}. Check that the app server can connect to the provider API and try again.");
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation(ex, "{Provider} training request was cancelled by the client.", provider);
                return new(null, "The AI request was interrupted. Please try again.");
            }
            logger.LogWarning(ex, "{Provider} training request timed out.", provider);
            return new(null, $"The {provider} request timed out. Try again; saved plans and workout history are unaffected.");
        }
        catch (JsonException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "{Provider} returned an unreadable training response.", provider);
            return new(null, $"{provider} returned an unreadable response. Check the model setting and try again.");
        }
    }

    private static async Task<HttpResponseMessage> SendQueuedWithRetryAsync(string provider, HttpClient client, string path, object body, bool useGemini, CancellationToken cancellationToken)
    {
        var gate = ProviderGates.GetOrAdd(provider, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await SendWithRetryAsync(client, path, body, useGemini, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<HttpResponseMessage> SendWithRetryAsync(HttpClient client, string path, object body, bool useGemini, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var response = await client.PostAsJsonAsync(path, body, cancellationToken);
            if (!useGemini || response.StatusCode != HttpStatusCode.ServiceUnavailable || attempt >= 2) return response;
            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(1000 * (1 << attempt) + Random.Shared.Next(0, 251)), cancellationToken);
        }
    }

    private static string? ReadChatCompletion(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0) return null;
        var choice = choices[0];
        if (choice.ValueKind != JsonValueKind.Object || !choice.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
            || !message.TryGetProperty("content", out var content)) return null;
        if (content.ValueKind == JsonValueKind.String) return content.GetString();
        return content.ValueKind == JsonValueKind.Array
            ? string.Join("\n", content.EnumerateArray().Where(part => part.ValueKind == JsonValueKind.Object && part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").ValueKind == JsonValueKind.String ? part.GetProperty("text").GetString() : null).Where(value => !string.IsNullOrWhiteSpace(value)))
            : null;
    }

    private static string? ReadResponsesText(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array) return null;
        return output.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object)
            .SelectMany(item => item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array ? content.EnumerateArray() : Enumerable.Empty<JsonElement>())
            .Where(part => part.ValueKind == JsonValueKind.Object && part.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "output_text")
            .Select(part => part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? ReadErrorCode(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object) return null;
            if (error.TryGetProperty("code", out var code))
            {
                if (code.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(code.GetString())) return code.GetString();
                if (code.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)) return code.ToString();
            }
            if (error.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(type.GetString())) return type.GetString();
            if (error.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String) return status.GetString();
            return null;
        }
        catch (JsonException) { return null; }
    }
}
