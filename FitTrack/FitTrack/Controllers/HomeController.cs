using System.Diagnostics;
using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class HomeController(AppDbContext db) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Index()
    {
        ViewData["UsePublicLayout"] = true;
        return View("Welcome");
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var profile = await db.UserProfiles.FirstOrDefaultAsync(item => item.IdentityUserId == userId, cancellationToken);
        if (profile is null || !profile.SetupCompleted) return RedirectToAction("Setup", "UserProfiles");
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var weeklyStart = startOfWeek.AddDays(-49);
        var weeklyEnd = startOfWeek.AddDays(7);
        var recentLogs = await db.WorkoutLogs.AsNoTracking().Where(item => item.UserProfileId == profile.Id)
            .OrderByDescending(item => item.CompletedAt).Take(8).ToListAsync(cancellationToken);
        var activePlan = await db.WorkoutPlans.AsNoTracking().Include(item => item.PlanExercises)
            .Where(item => item.UserProfileId == profile.Id && item.IsActive).OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfNextMonth = startOfMonth.AddMonths(1);
        var weekLogs = await db.WorkoutLogs.AsNoTracking().Where(item => item.UserProfileId == profile.Id
            && item.CompletedAt >= startOfWeek.ToUniversalTime() && item.CompletedAt < today.AddDays(1).ToUniversalTime())
            .Select(item => new { item.WorkoutSessionId, item.CompletedAt }).ToListAsync(cancellationToken);
        var monthDates = await db.WorkoutLogs.AsNoTracking().Where(item => item.UserProfileId == profile.Id
            && item.CompletedAt >= startOfMonth.ToUniversalTime() && item.CompletedAt < startOfNextMonth.ToUniversalTime())
            .Select(item => item.CompletedAt).ToListAsync(cancellationToken);
        var activeDays = monthDates.Select(item => item.ToLocalTime().Date).Distinct().Count();
        var chartLogs = await db.WorkoutLogs.AsNoTracking().Where(item => item.UserProfileId == profile.Id
            && item.CompletedAt >= weeklyStart.ToUniversalTime() && item.CompletedAt < weeklyEnd.ToUniversalTime())
            .ToListAsync(cancellationToken);
        var weeklyActivity = Enumerable.Range(0, 8).Select(index =>
        {
            var weekStart = weeklyStart.AddDays(index * 7);
            var entries = chartLogs.Where(item =>
            {
                var localDate = item.CompletedAt.ToLocalTime();
                return localDate >= weekStart && localDate < weekStart.AddDays(7);
            }).ToList();
            return new WeeklyProgressPoint
            {
                WeekStarting = DateOnly.FromDateTime(weekStart),
                Workouts = entries.Where(item => !string.IsNullOrEmpty(item.WorkoutSessionId))
                    .Select(item => item.WorkoutSessionId).Distinct().Count(),
                ActiveDays = entries.Select(item => DateOnly.FromDateTime(item.CompletedAt.ToLocalTime())).Distinct().Count(),
                Sets = entries.Sum(item => (int)item.SetsCompleted),
                ExternalVolume = entries.Sum(item => item.WeightUsed * item.SetsCompleted * item.RepetitionsCompleted)
            };
        }).ToList();
        return View("Index", new DashboardViewModel
        {
            Profile = profile,
            ActivePlan = activePlan,
            RecentWorkouts = recentLogs,
            WeeklyActivity = weeklyActivity,
            CompletedWorkoutsThisWeek = weekLogs.Select(item => string.IsNullOrWhiteSpace(item.WorkoutSessionId) ? item.CompletedAt.ToLocalTime().ToString("yyyy-MM-dd") : item.WorkoutSessionId).Distinct().Count(),
            ActiveDaysThisMonth = activeDays
        });
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
