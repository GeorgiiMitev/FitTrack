using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class WorkoutLogsController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var logs = await db.WorkoutLogs.AsNoTracking().Include(item => item.WorkoutPlan)
            .Where(item => item.UserProfileId == profile.Id).OrderByDescending(item => item.CompletedAt).ToListAsync(cancellationToken);
        return View(logs);
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
            RepetitionsCompleted = model.RepetitionsCompleted, WeightUsed = model.WeightUsed, Notes = model.Notes.Trim()
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
