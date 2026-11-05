using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class ProgressEntriesController(AppDbContext db) : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Progress");

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        await PopulateExerciseSuggestionsAsync(profile.Id, cancellationToken);
        return View(new ProgressEntry { Date = DateOnly.FromDateTime(DateTime.Today), ExerciseName = string.Empty });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ExerciseName,MaxWeight,Date")] ProgressEntry entry, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var suggestions = await GetExerciseSuggestionsAsync(profile.Id, cancellationToken);
        entry.ExerciseName = CanonicalizeExerciseName(entry.ExerciseName, suggestions);
        if (entry.Date > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(entry.Date), "Choose today or an earlier date.");
        var duplicateExists = await db.ProgressEntries.AsNoTracking()
            .Where(item => item.UserProfileId == profile.Id && item.Date == entry.Date)
            .Select(item => item.ExerciseName).ToListAsync(cancellationToken);
        if (duplicateExists.Any(name => name.Equals(entry.ExerciseName, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(string.Empty, "A measurement for this lift is already saved on that date. Edit the existing entry or choose another date.");
        if (!ModelState.IsValid)
        {
            ViewBag.ExerciseSuggestions = suggestions;
            return View(entry);
        }
        entry.UserProfileId = profile.Id;
        db.ProgressEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        TempData["ProgressEntrySaved"] = "Progress measurement saved.";
        return RedirectToAction("Index", "Progress");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var entry = await db.ProgressEntries.FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        if (entry is null) return NotFound();
        await PopulateExerciseSuggestionsAsync(profile.Id, cancellationToken);
        return View(entry);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ExerciseName,MaxWeight,Date")] ProgressEntry input, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (id != input.Id) return NotFound();
        var entry = await db.ProgressEntries.FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        if (entry is null) return NotFound();
        var suggestions = await GetExerciseSuggestionsAsync(profile.Id, cancellationToken);
        input.ExerciseName = CanonicalizeExerciseName(input.ExerciseName, suggestions);
        if (input.Date > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(input.Date), "Choose today or an earlier date.");
        var duplicateExists = await db.ProgressEntries.AsNoTracking()
            .Where(item => item.UserProfileId == profile.Id && item.Date == input.Date && item.Id != id)
            .Select(item => item.ExerciseName).ToListAsync(cancellationToken);
        if (duplicateExists.Any(name => name.Equals(input.ExerciseName, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(string.Empty, "A measurement for this lift is already saved on that date. Edit that entry or choose another date.");
        if (!ModelState.IsValid)
        {
            ViewBag.ExerciseSuggestions = suggestions;
            return View(input);
        }
        entry.MaxWeight = input.MaxWeight;
        entry.ExerciseName = input.ExerciseName;
        entry.Date = input.Date;
        await db.SaveChangesAsync(cancellationToken);
        TempData["ProgressEntrySaved"] = "Progress measurement updated.";
        return RedirectToAction("Index", "Progress");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var entry = await db.ProgressEntries.FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        if (entry is null) return NotFound();
        db.ProgressEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        TempData["ProgressEntrySaved"] = "Progress measurement removed.";
        return RedirectToAction("Index", "Progress");
    }

    private Task<UserProfile?> CurrentProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.UserProfiles.FirstOrDefaultAsync(profile => profile.IdentityUserId == userId, cancellationToken);
    }

    private async Task PopulateExerciseSuggestionsAsync(int profileId, CancellationToken cancellationToken) =>
        ViewBag.ExerciseSuggestions = await GetExerciseSuggestionsAsync(profileId, cancellationToken);

    private async Task<List<string>> GetExerciseSuggestionsAsync(int profileId, CancellationToken cancellationToken)
    {
        var previousMeasurements = await db.ProgressEntries.AsNoTracking()
            .Where(item => item.UserProfileId == profileId).Select(item => item.ExerciseName).ToListAsync(cancellationToken);
        var completedExercises = await db.WorkoutLogs.AsNoTracking()
            .Where(item => item.UserProfileId == profileId).Select(item => item.ExerciseName).ToListAsync(cancellationToken);
        return previousMeasurements.Concat(completedExercises)
            .Select(name => name.Trim()).Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string CanonicalizeExerciseName(string? name, IReadOnlyCollection<string> suggestions)
    {
        var normalized = name?.Trim() ?? string.Empty;
        return suggestions.FirstOrDefault(option => option.Equals(normalized, StringComparison.OrdinalIgnoreCase)) ?? normalized;
    }
}
