using System.Text.Json;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Services;

public sealed record PlanBuildResult(WorkoutPlan? Plan, string? Error)
{
    public bool Success => Plan is not null && Error is null;
}

public interface IWorkoutPlanService
{
    Task<PlanBuildResult> GenerateAiAsync(UserProfile profile, IReadOnlyList<ExerciseSummary> catalog, string userId, CancellationToken cancellationToken = default);
    Task<PlanBuildResult> CreateManualAsync(UserProfile profile, IReadOnlyList<ExerciseSummary> catalog, string planName, string goal, IReadOnlyCollection<int> exerciseIds, CancellationToken cancellationToken = default);
}

public sealed class WorkoutPlanService(AppDbContext db, IAITrainingService ai) : IWorkoutPlanService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PlanBuildResult> CreateManualAsync(UserProfile profile, IReadOnlyList<ExerciseSummary> catalog, string planName, string goal,
        IReadOnlyCollection<int> exerciseIds, CancellationToken cancellationToken = default)
    {
        var choices = catalog.Where(item => exerciseIds.Contains(item.Id)).GroupBy(item => item.Id).Select(group => group.First()).ToList();
        if (choices.Count == 0) return new(null, "Select at least one exercise from the current wger library.");
        var eligible = choices.Where(item => IsEquipmentAvailable(item, profile.Equipment)).ToList();
        if (eligible.Count != choices.Count) return new(null, "One or more selected exercises require equipment outside your saved profile. Update your profile or choose suitable exercises.");
        var days = GetPreferredWeekdays(profile);
        if (days.Count == 0) return new(null, "Add at least one preferred training day to your profile first.");
        var plan = new WorkoutPlan { UserProfileId = profile.Id, Name = string.IsNullOrWhiteSpace(planName) ? "My workout plan" : planName.Trim(), Goal = string.IsNullOrWhiteSpace(goal) ? profile.Goal : goal.Trim(), IsActive = false };
        for (var i = 0; i < eligible.Count; i++)
        {
            var exercise = eligible[i];
            plan.PlanExercises.Add(new PlanExercise
            {
                WgerExerciseId = exercise.Id,
                ExerciseName = exercise.Name,
                DayOfTheWeek = (byte)days[i % days.Count],
                Sets = profile.ExperienceLevel == FitnessExperienceLevel.Beginner ? (byte)3 : (byte)4,
                Repetitions = profile.Goal.Contains("strength", StringComparison.OrdinalIgnoreCase) ? (byte)6 : (byte)10,
                RestSeconds = profile.Goal.Contains("strength", StringComparison.OrdinalIgnoreCase) ? (byte)120 : (byte)75
            });
        }
        await db.WorkoutPlans.AddAsync(plan, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(plan, null);
    }

    public async Task<PlanBuildResult> GenerateAiAsync(UserProfile profile, IReadOnlyList<ExerciseSummary> catalog, string userId, CancellationToken cancellationToken = default)
    {
        var eligible = catalog.Where(exercise => IsEquipmentAvailable(exercise, profile.Equipment)).Take(250).ToList();
        if (eligible.Count < 3) return new(null, "wger has too few matching exercises for the equipment in your profile. If you can do bodyweight exercises, add Bodyweight in Profile → Available equipment. Otherwise, select the equipment you actually have and retry.");
        var days = GetPreferredWeekdays(profile);
        if (days.Count == 0) return new(null, "Add preferred training days to your profile first.");
        var candidates = eligible.Select(exercise => new { id = exercise.Id, name = exercise.Name, equipment = exercise.Equipment, muscles = exercise.Muscles }).ToList();
        var aiResult = await ai.GeneratePlanDraftAsync(userId, JsonSerializer.Serialize(candidates, JsonOptions), cancellationToken);
        if (!aiResult.Success) return new(null, aiResult.Error ?? "The AI plan could not be generated.");
        if (!TryParseDraft(aiResult.Text!, eligible, days, out var planName, out var planExercises, out var error)) return new(null, error);

        var plan = new WorkoutPlan { UserProfileId = profile.Id, Name = planName.Length > 0 ? planName : "AI training plan", Goal = profile.Goal, IsAiGenerated = true, IsActive = false };
        foreach (var item in planExercises) plan.PlanExercises.Add(item);
        await db.WorkoutPlans.AddAsync(plan, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(plan, null);
    }

    public static bool IsEquipmentAvailable(ExerciseSummary exercise, IReadOnlyCollection<string> profileEquipment)
    {
        // wger represents equipment-free movements with an empty equipment list on some
        // records, and with an explicit "none" entry on others.
        if (exercise.Equipment.Count == 0)
            return profileEquipment.Any(available => Normalize(available) is "bodyweight" or "none" or "noequipment");
        return exercise.Equipment.All(required =>
            Normalize(required) is "none" or "bodyweight" or "noequipment"
                ? profileEquipment.Any(available => Normalize(available) is "bodyweight" or "none" or "noequipment")
                : profileEquipment.Any(available => EquivalentEquipment(required, available)));
    }

    public static List<int> GetPreferredWeekdays(UserProfile profile)
    {
        var result = new List<int>();
        foreach (var day in profile.PreferredTrainingDays)
            if (Enum.TryParse<DayOfWeek>(day, true, out var weekday) && !result.Contains((int)weekday)) result.Add((int)weekday);
        if (result.Count == 0 && profile.TrainingDaysPerWeek > 0)
            result.AddRange(Enumerable.Range(1, Math.Min(profile.TrainingDaysPerWeek, (byte)7)).Select(day => day % 7));
        return result;
    }

    private static bool EquivalentEquipment(string required, string available)
    {
        var req = Normalize(required);
        var have = Normalize(available);
        if (req == have || (req.EndsWith('s') && req[..^1] == have) || (have.EndsWith('s') && have[..^1] == req)) return true;
        if (req is "dumbbell" or "dumbbells") return have is "dumbbell" or "dumbbells";
        if (req is "szbar" or "ezbar" or "ezcurlbar") return have is "szbar" or "ezbar" or "ezcurlbar";
        if (req is "gymmat" or "exercisegymmat") return have is "gymmat" or "exercisegymmat" or "exercisemat";
        if (req is "bench" or "exercisebench") return have is "bench" or "flatbench" or "adjustablebench" or "inclinebench" or "declinebench";
        if (req is "inclinebench" or "declinebench") return have is "adjustablebench" or "inclinebench" or "declinebench";
        return req switch
        {
            "barbell" => have is "barbell" or "barbells",
            "weightmachine" or "machine" => have.EndsWith("machine") || have.EndsWith("machines"),
            _ => false
        };
    }

    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool TryParseDraft(string response, IReadOnlyList<ExerciseSummary> catalog, IReadOnlyCollection<int> allowedDays,
        out string planName, out List<PlanExercise> exercises, out string error)
    {
        planName = string.Empty;
        exercises = new List<PlanExercise>();
        error = "The AI returned a plan that could not be validated. Please try generating it again.";
        try
        {
            var json = response.Trim();
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = json.IndexOf('\n');
                var finalFence = json.LastIndexOf("```", StringComparison.Ordinal);
                if (firstNewline >= 0 && finalFence > firstNewline) json = json[(firstNewline + 1)..finalFence].Trim();
            }
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("days", out var days) || days.ValueKind != JsonValueKind.Array) return false;
            planName = root.TryGetProperty("planName", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString()?.Trim() ?? string.Empty : string.Empty;
            var candidates = catalog.ToDictionary(item => item.Id);
            var selected = new HashSet<int>();
            foreach (var day in days.EnumerateArray())
            {
                if (day.ValueKind != JsonValueKind.Object || !day.TryGetProperty("weekday", out var weekdayJson) || weekdayJson.ValueKind != JsonValueKind.String
                    || !Enum.TryParse<DayOfWeek>(weekdayJson.GetString(), true, out var weekday) || !allowedDays.Contains((int)weekday)
                    || !day.TryGetProperty("exercises", out var dayExercises) || dayExercises.ValueKind != JsonValueKind.Array) return false;
                if (dayExercises.GetArrayLength() > 8) return false;
                foreach (var assignment in dayExercises.EnumerateArray())
                {
                    if (assignment.ValueKind != JsonValueKind.Object || !ReadInt(assignment, "exerciseId", out var id) || !candidates.TryGetValue(id, out var candidate) || !selected.Add(id)
                        || !ReadInt(assignment, "sets", out var sets) || sets is < 1 or > 6
                        || !ReadInt(assignment, "repetitions", out var repetitions) || repetitions is < 1 or > 30
                        || !ReadInt(assignment, "restSeconds", out var rest) || rest is < 15 or > 300) return false;
                    exercises.Add(new PlanExercise
                    {
                        WgerExerciseId = id, ExerciseName = candidate.Name, DayOfTheWeek = (byte)weekday,
                        Sets = (byte)sets, Repetitions = (byte)repetitions, RestSeconds = (byte)rest
                    });
                }
            }
            if (exercises.Count < 3 || exercises.Count > 42) return false;
            return true;
        }
        catch (JsonException) { return false; }
    }

    private static bool ReadInt(JsonElement element, string key, out int value)
    {
        value = 0;
        return element.TryGetProperty(key, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value);
    }
}
