using System.Text.Json.Serialization;

namespace FitTrack.Models;

public sealed class WgerPageDto<T>
{
    public int Count { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
    public List<T> Results { get; set; } = new();
}

public sealed class WgerExerciseInfoDto
{
    public int Id { get; set; }
    public WgerNamedDto? Category { get; set; }
    public List<WgerNamedDto> Muscles { get; set; } = new();
    [JsonPropertyName("muscles_secondary")] public List<WgerNamedDto> SecondaryMuscles { get; set; } = new();
    public List<WgerNamedDto> Equipment { get; set; } = new();
    public List<WgerExerciseTranslationDto> Translations { get; set; } = new();
    public List<WgerImageDto> Images { get; set; } = new();
    public WgerLicenseDto? License { get; set; }
    [JsonPropertyName("license_author")] public string? LicenseAuthor { get; set; }
}

public sealed class WgerNamedDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    [JsonPropertyName("name_en")] public string? EnglishName { get; set; }
}

public sealed class WgerExerciseTranslationDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int Language { get; set; }
}

public sealed class WgerLanguageDto
{
    public int Id { get; set; }
    [JsonPropertyName("short_name")] public string? ShortName { get; set; }
}

public sealed class WgerImageDto
{
    public string? Image { get; set; }
    public Dictionary<string, string>? Thumbnails { get; set; }
    [JsonPropertyName("is_main")] public bool IsMain { get; set; }
    [JsonPropertyName("license_author")] public string? LicenseAuthor { get; set; }
    [JsonPropertyName("license_title")] public string? LicenseTitle { get; set; }
    [JsonPropertyName("license_object_url")] public string? LicenseUrl { get; set; }
}

public sealed class WgerLicenseDto
{
    public string? Name { get; set; }
    [JsonPropertyName("full_name")] public string? FullName { get; set; }
    public string? Url { get; set; }
}

public sealed record ExerciseSummary(
    int Id,
    string Name,
    string Description,
    string Category,
    IReadOnlyList<string> Muscles,
    IReadOnlyList<string> Equipment,
    string? ImageUrl,
    string? ImageThumbnailUrl,
    string? LicenseAuthor,
    string? LicenseTitle,
    string? LicenseUrl);

public sealed record ExerciseCatalogResult(IReadOnlyList<ExerciseSummary> Exercises, bool IsUsingCache, string? Error);
