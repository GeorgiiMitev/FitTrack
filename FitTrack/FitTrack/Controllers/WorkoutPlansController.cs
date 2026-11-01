using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using FitTrack.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class WorkoutPlansController(AppDbContext db, IExerciseApiService exercises, IWorkoutPlanService plans) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var items = await db.WorkoutPlans.AsNoTracking().Include(plan => plan.PlanExercises)
            .Where(plan => plan.UserProfileId == profile.Id).OrderByDescending(plan => plan.CreatedAt).ToListAsync(cancellationToken);
        ViewBag.ApiNotice = TempData["PlanError"] as string;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var plan = await db.WorkoutPlans.AsNoTracking().Include(item => item.PlanExercises)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        return plan is null ? NotFound() : View(plan);
    }

    [HttpGet]
    public async Task<IActionResult> Manual(string? search, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var catalog = await exercises.GetExercisesAsync(cancellationToken);
        var suitable = catalog.Exercises.Where(item => WorkoutPlanService.IsEquipmentAvailable(item, profile.Equipment));
        if (!string.IsNullOrWhiteSpace(search)) suitable = suitable.Where(item => item.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        var model = new ManualPlanViewModel
        {
            Goal = profile.Goal,
            AvailableExercises = suitable.OrderBy(item => item.Name).Take(300).ToList(),
            Error = catalog.Error
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Manual(ManualPlanViewModel model, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var catalog = await exercises.GetExercisesAsync(cancellationToken);
        if (ModelState.IsValid)
        {
            var result = await plans.CreateManualAsync(profile, catalog.Exercises, model.PlanName, model.Goal, model.ExerciseIds, cancellationToken);
            if (result.Success) return RedirectToAction(nameof(Details), new { id = result.Plan!.Id });
            model.Error = result.Error;
        }
        model.AvailableExercises = catalog.Exercises.Where(item => WorkoutPlanService.IsEquipmentAvailable(item, profile.Equipment))
            .OrderBy(item => item.Name).Take(300).ToList();
        if (!string.IsNullOrWhiteSpace(catalog.Error)) model.Error ??= catalog.Error;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAi(CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var catalog = await exercises.GetExercisesAsync(cancellationToken);
        if (catalog.Exercises.Count == 0)
        {
            TempData["PlanError"] = catalog.Error ?? "Exercise catalog is unavailable.";
            return RedirectToAction(nameof(Index));
        }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await plans.GenerateAiAsync(profile, catalog.Exercises, userId, cancellationToken);
        if (!result.Success) TempData["PlanError"] = result.Error;
        return result.Success ? RedirectToAction(nameof(Details), new { id = result.Plan!.Id }) : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var plansForUser = await db.WorkoutPlans.Where(plan => plan.UserProfileId == profile.Id).ToListAsync(cancellationToken);
        var selected = plansForUser.FirstOrDefault(plan => plan.Id == id);
        if (selected is null) return NotFound();
        foreach (var plan in plansForUser) plan.IsActive = plan.Id == selected.Id;
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var plan = await db.WorkoutPlans.AsNoTracking().Include(item => item.PlanExercises)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id && item.IsActive, cancellationToken);
        if (plan is null) return NotFound("Activate your own plan before logging a workout.");
        return View(ToCompletionModel(plan));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(WorkoutCompletionViewModel model, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var plan = await db.WorkoutPlans.Include(item => item.PlanExercises)
            .FirstOrDefaultAsync(item => item.Id == model.PlanId && item.UserProfileId == profile.Id && item.IsActive, cancellationToken);
        if (plan is null) return NotFound();
        var allowed = plan.PlanExercises.ToDictionary(item => item.Id);
        var selected = model.Exercises.Where(item => item.Completed).ToList();
        if (selected.Count == 0) ModelState.AddModelError(string.Empty, "Mark at least one exercise complete to save the workout.");
        if (selected.GroupBy(item => item.PlanExerciseId).Any(group => group.Count() > 1)
            || selected.Any(item => !allowed.ContainsKey(item.PlanExerciseId) || item.SetsCompleted is < 1 or > 20 || item.RepetitionsCompleted is < 1 or > 100 || item.WeightUsed < 0))
            ModelState.AddModelError(string.Empty, "Exercise details were invalid. Check completed sets, reps, and weight.");
        if (!ModelState.IsValid)
        {
            model.PlanName = plan.Name;
            foreach (var item in model.Exercises)
                if (allowed.TryGetValue(item.PlanExerciseId, out var prescribed)) { item.ExerciseName = prescribed.ExerciseName; item.PrescribedSets = prescribed.Sets; item.PrescribedRepetitions = prescribed.Repetitions; }
            return View(model);
        }
        var sessionId = Guid.NewGuid().ToString("N");
        var completedAt = DateTime.UtcNow;
        foreach (var item in selected)
        {
            var prescribed = allowed[item.PlanExerciseId];
            db.WorkoutLogs.Add(new WorkoutLog
            {
                UserProfileId = profile.Id, WorkoutPlanId = plan.Id, PlanExerciseId = prescribed.Id, WorkoutSessionId = sessionId,
                ExerciseName = prescribed.ExerciseName, CompletedAt = completedAt, SetsCompleted = item.SetsCompleted,
                RepetitionsCompleted = item.RepetitionsCompleted, WeightUsed = item.WeightUsed, Notes = item.Notes?.Trim() ?? string.Empty
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        TempData["WorkoutSaved"] = "Workout saved to your history.";
        return RedirectToAction("Index", "Progress", new { date = DateTime.Now.ToString("yyyy-MM-dd") });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var plan = await db.WorkoutPlans.FirstOrDefaultAsync(item => item.Id == id && item.UserProfileId == profile.Id, cancellationToken);
        if (plan is null) return NotFound();
        db.WorkoutPlans.Remove(plan);
        await db.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private Task<UserProfile?> CurrentProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.UserProfiles.FirstOrDefaultAsync(profile => profile.IdentityUserId == userId, cancellationToken);
    }

    private static WorkoutCompletionViewModel ToCompletionModel(WorkoutPlan plan) => new()
    {
        PlanId = plan.Id,
        PlanName = plan.Name,
        Exercises = plan.PlanExercises.OrderBy(item => item.DayOfTheWeek).ThenBy(item => item.Id).Select(item => new WorkoutCompletionExercise
        {
            PlanExerciseId = item.Id, ExerciseName = item.ExerciseName, PrescribedSets = item.Sets,
            PrescribedRepetitions = item.Repetitions, SetsCompleted = item.Sets, RepetitionsCompleted = item.Repetitions
        }).ToList()
    };
}
