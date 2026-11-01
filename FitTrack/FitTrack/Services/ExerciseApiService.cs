using System.Net.Http.Json;
using System.Text.Json;
using FitTrack.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FitTrack.Services;

public interface IExerciseApiService
{
    Task<ExerciseCatalogResult> GetExercisesAsync(CancellationToken cancellationToken = default);
}

public sealed class ExerciseApiService(IHttpClientFactory clients, IMemoryCache cache, ILogger<ExerciseApiService> logger) : IExerciseApiService
{
    private const string FreshKey = "wger-exercise-catalog-v1";
    private const string StaleKey = "wger-exercise-catalog-stale-v1";
    private static readonly TimeSpan FreshFor = TimeSpan.FromHours(8);
    private static readonly TimeSpan StaleFor = TimeSpan.FromDays(14);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    private int? englishLanguageId;

    public async Task<ExerciseCatalogResult> GetExercisesAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(FreshKey, out IReadOnlyList<ExerciseSummary>? fresh) && fresh is { Count: > 0 })
            return new(fresh, true, null);
        try
        {
            var exercises = await LoadAllAsync(cancellationToken);
            if (exercises.Count > 0)
            {
                cache.Set(FreshKey, exercises, FreshFor);
                cache.Set(StaleKey, exercises, StaleFor);
                return new(exercises, false, null);
            }
            return GetStaleOrEmpty("wger returned no exercises. Try again later.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Unable to retrieve the wger exercise catalog.");
            return GetStaleOrEmpty("The wger exercise catalog is temporarily unavailable. Check your internet connection and try again.");
        }
    }

    private ExerciseCatalogResult GetStaleOrEmpty(string message) =>
        cache.TryGetValue(StaleKey, out IReadOnlyList<ExerciseSummary>? stale) && stale is { Count: > 0 }
            ? new(stale, true, "wger is unavailable. Showing the last successful catalog copy.")
            : new(Array.Empty<ExerciseSummary>(), false, message);

    private async Task<List<ExerciseSummary>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var client = clients.CreateClient("Wger");
        if (englishLanguageId is null)
        {
            try
            {
                var languagePage = await client.GetFromJsonAsync<WgerPageDto<WgerLanguageDto>>("language/?limit=100", JsonOptions, cancellationToken);
                englishLanguageId = languagePage?.Results.FirstOrDefault(language => string.Equals(language.ShortName, "en", StringComparison.OrdinalIgnoreCase))?.Id;
            }
            catch (HttpRequestException) { /* Use the first translation if language metadata is temporarily unavailable. */ }
        }

        var next = "exerciseinfo/?limit=100";
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<ExerciseSummary>();
        for (var pageIndex = 0; !string.IsNullOrWhiteSpace(next) && pageIndex < 20; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!visited.Add(next)) break;
            using var response = await client.GetAsync(next, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<WgerPageDto<WgerExerciseInfoDto>>(JsonOptions, cancellationToken);
            if (page is null) throw new JsonException("wger returned an empty exercise page.");
            foreach (var source in page.Results)
            {
                var translation = (englishLanguageId is int englishId ? source.Translations.FirstOrDefault(item => item.Language == englishId) : null)
                    ?? source.Translations.FirstOrDefault();
                var name = Clean(translation?.Name);
                if (name.Length == 0) continue;
                var image = source.Images.FirstOrDefault(item => item.IsMain) ?? source.Images.FirstOrDefault();
                output.Add(new ExerciseSummary(source.Id, name, Clean(translation?.Description), Clean(source.Category?.Name),
                    source.Muscles.Concat(source.SecondaryMuscles)
                        .Select(muscle => Clean(muscle.EnglishName ?? muscle.Name)).Where(value => value.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    source.Equipment.Select(item => Clean(item.Name)).Where(value => value.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    image?.Image, image?.Thumbnails?.GetValueOrDefault("small") ?? image?.Thumbnails?.GetValueOrDefault("medium"),
                    image?.LicenseAuthor ?? source.LicenseAuthor, image?.LicenseTitle ?? source.License?.FullName ?? source.License?.Name,
                    image?.LicenseUrl ?? source.License?.Url));
            }
            next = page.Next;
        }
        return output.GroupBy(item => item.Id).Select(group => group.First()).ToList();
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
