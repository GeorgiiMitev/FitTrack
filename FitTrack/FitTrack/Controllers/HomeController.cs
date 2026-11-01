using System.Diagnostics;
using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class HomeController(AppDbContext db) : Controller
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true) return View("Welcome");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await db.UserProfiles.FirstOrDefaultAsync(item => item.IdentityUserId == userId, cancellationToken);
        if (profile is null || !profile.SetupCompleted) return RedirectToAction("Setup", "UserProfiles");
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var logs = await db.WorkoutLogs.AsNoTracking().Where(item => item.UserProfileId == profile.Id).OrderByDescending(item => item.CompletedAt).ToListAsync(cancellationToken);
        var activePlan = await db.WorkoutPlans.AsNoTracking().Include(item => item.PlanExercises)
            .Where(item => item.UserProfileId == profile.Id && item.IsActive).OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var weekLogs = logs.Where(item => item.CompletedAt.ToLocalTime().Date >= startOfWeek).ToList();
        var activeDays = logs.Where(item => item.CompletedAt.ToLocalTime().Year == today.Year && item.CompletedAt.ToLocalTime().Month == today.Month)
            .Select(item => item.CompletedAt.ToLocalTime().Date).Distinct().Count();
        return View(new DashboardViewModel
        {
            Profile = profile,
            ActivePlan = activePlan,
            RecentWorkouts = logs.Take(8).ToList(),
            CompletedWorkoutsThisWeek = weekLogs.Select(item => string.IsNullOrWhiteSpace(item.WorkoutSessionId) ? item.CompletedAt.ToLocalTime().ToString("yyyy-MM-dd") : item.WorkoutSessionId).Distinct().Count(),
            ActiveDaysThisMonth = activeDays
        });
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
