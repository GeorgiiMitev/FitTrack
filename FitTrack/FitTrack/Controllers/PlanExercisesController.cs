using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class PlanExercisesController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var exercises = await db.PlanExercises.AsNoTracking().Include(item => item.WorkoutPlan)
            .Where(item => item.WorkoutPlan != null && item.WorkoutPlan.UserProfileId == profile.Id)
            .OrderBy(item => item.WorkoutPlan!.Name).ThenBy(item => item.DayOfTheWeek).ToListAsync(cancellationToken);
        return View(exercises);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var item = await db.PlanExercises.AsNoTracking().Include(exercise => exercise.WorkoutPlan)
            .FirstOrDefaultAsync(exercise => exercise.Id == id && exercise.WorkoutPlan != null && exercise.WorkoutPlan.UserProfileId == profile.Id, cancellationToken);
        return item is null ? NotFound() : View(item);
    }

    private Task<UserProfile?> CurrentProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.UserProfiles.FirstOrDefaultAsync(profile => profile.IdentityUserId == userId, cancellationToken);
    }
}
