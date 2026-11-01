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
    public IActionResult Create() => View(new ProgressEntry { Date = DateOnly.FromDateTime(DateTime.Today), ExerciseName = string.Empty });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ExerciseName,MaxWeight,Date")] ProgressEntry entry, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (!ModelState.IsValid) return View(entry);
        entry.UserProfileId = profile.Id;
        entry.ExerciseName = entry.ExerciseName.Trim();
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
        return entry is null ? NotFound() : View(entry);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ExerciseName,MaxWeight,Date")] ProgressEntry input, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        if (id != input.Id) return NotFound();
        var entry = await db.ProgressEntries.FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        if (entry is null) return NotFound();
        if (!ModelState.IsValid) return View(input);
        entry.MaxWeight = input.MaxWeight;
        entry.ExerciseName = input.ExerciseName.Trim();
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
}
