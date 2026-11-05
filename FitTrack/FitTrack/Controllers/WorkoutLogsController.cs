using System.Security.Claims;
using System.Globalization;
using System.Text;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class WorkoutLogsController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, DateOnly? from, DateOnly? to, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var query = db.WorkoutLogs.AsNoTracking().Include(item => item.WorkoutPlan).Where(item => item.UserProfileId == profile.Id);
        search = search?.Trim();
        if (from is not null) query = query.Where(item => item.CompletedAt >= from.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());
        if (to is not null) query = query.Where(item => item.CompletedAt < to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUniversalTime());
        sort = sort == "oldest" ? "oldest" : "newest";

        var searchTerms = string.IsNullOrWhiteSpace(search)
            ? Array.Empty<string>()
            : search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeSearchText)
                .Where(term => term.Length > 0)
                .ToArray();

        if (searchTerms.Length > 0)
        {
            var matches = await query.ToListAsync(cancellationToken);
            matches = matches.Where(item =>
            {
                var exerciseName = NormalizeSearchText(item.ExerciseName);
                var planName = NormalizeSearchText(item.WorkoutPlan?.Name ?? string.Empty);
                return searchTerms.All(term => exerciseName.Contains(term, StringComparison.Ordinal)
                    || planName.Contains(term, StringComparison.Ordinal));
            }).ToList();

            var totalMatches = matches.Count;
            page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(totalMatches / 20d)));
            var matchingLogs = sort == "oldest"
                ? matches.OrderBy(item => item.CompletedAt)
                : matches.OrderByDescending(item => item.CompletedAt);
            return View(new WorkoutHistoryViewModel
            {
                Logs = matchingLogs.Skip((page - 1) * 20).Take(20).ToList(),
                Search = search, From = from, To = to, Sort = sort, Page = page, TotalItems = totalMatches
            });
        }

        var totalItems = await query.CountAsync(cancellationToken);
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(totalItems / 20d)));
        query = sort == "oldest" ? query.OrderBy(item => item.CompletedAt) : query.OrderByDescending(item => item.CompletedAt);
        var logs = await query.Skip((page - 1) * 20).Take(20).ToListAsync(cancellationToken);
        return View(new WorkoutHistoryViewModel { Logs = logs, Search = search, From = from, To = to, Sort = sort, Page = page, TotalItems = totalItems });
    }

    private static string NormalizeSearchText(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var normalized = new StringBuilder(decomposed.Length);
        foreach (var rune in decomposed.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
                continue;
            if (Rune.IsLetterOrDigit(rune)) normalized.Append(Rune.ToLowerInvariant(rune).ToString());
        }
        return normalized.ToString();
    }

    [HttpGet]
    public IActionResult Create() => View(new ManualWorkoutLogViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManualWorkoutLogViewModel model, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        db.WorkoutLogs.Add(new WorkoutLog
        {
            UserProfileId = profile.Id, ExerciseName = model.ExerciseName.Trim(),
            CompletedAt = DateTime.SpecifyKind(model.CompletedAt, DateTimeKind.Local).ToUniversalTime(),
            WorkoutSessionId = Guid.NewGuid().ToString("N"), SetsCompleted = model.SetsCompleted,
            RepetitionsCompleted = model.RepetitionsCompleted, WeightUsed = model.WeightUsed, Notes = model.Notes?.Trim() ?? string.Empty
        });
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var log = await db.WorkoutLogs.AsNoTracking().Include(item => item.WorkoutPlan)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        return log is null ? NotFound() : View(log);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var log = await db.WorkoutLogs.FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        if (log is null) return NotFound();
        db.WorkoutLogs.Remove(log);
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private Task<UserProfile?> CurrentProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.UserProfiles.FirstOrDefaultAsync(profile => profile.IdentityUserId == userId, cancellationToken);
    }
}
