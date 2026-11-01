using System.Globalization;
using System.Security.Claims;
using FitTrack.Data;
using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Controllers;

public sealed class ProgressController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, string? date, CancellationToken cancellationToken)
    {
        var profile = await CurrentProfileAsync(cancellationToken);
        if (profile is null) return NotFound();
        var now = DateTime.Today;
        var selectedYear = year is >= 2000 and <= 2100 ? year.Value : now.Year;
        var selectedMonth = month is >= 1 and <= 12 ? month.Value : now.Month;
        DateOnly? selectedDate = DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
        if (selectedDate is null && year is null && month is null) selectedDate = DateOnly.FromDateTime(now);
        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        var monthEnd = monthStart.AddMonths(1);
        var activityLogs = await db.WorkoutLogs.AsNoTracking().Where(log => log.UserProfileId == profile.Id
            && log.CompletedAt >= monthStart.ToUniversalTime() && log.CompletedAt < monthEnd.ToUniversalTime()).ToListAsync(cancellationToken);
        var activeDates = activityLogs.Select(log => DateOnly.FromDateTime(log.CompletedAt.ToLocalTime())).ToHashSet();
        var localMonday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        var weeklyStart = localMonday.AddDays(-49);
        var weeklyEnd = localMonday.AddDays(7);
        var recentLogs = await db.WorkoutLogs.AsNoTracking().Where(log => log.UserProfileId == profile.Id
            && log.CompletedAt >= weeklyStart.ToUniversalTime() && log.CompletedAt < weeklyEnd.ToUniversalTime())
            .OrderByDescending(log => log.CompletedAt).ToListAsync(cancellationToken);
        var weeklyActivity = Enumerable.Range(0, 8).Select(index =>
        {
            var weekStart = weeklyStart.AddDays(index * 7);
            var entries = recentLogs.Where(log =>
            {
                var localDate = log.CompletedAt.ToLocalTime();
                return localDate >= weekStart && localDate < weekStart.AddDays(7);
            }).ToList();
            return new WeeklyProgressPoint
            {
                WeekStarting = DateOnly.FromDateTime(weekStart),
                Workouts = entries.Where(log => !string.IsNullOrEmpty(log.WorkoutSessionId)).Select(log => log.WorkoutSessionId).Distinct().Count(),
                ActiveDays = entries.Select(log => DateOnly.FromDateTime(log.CompletedAt.ToLocalTime())).Distinct().Count(),
                Sets = entries.Sum(log => (int)log.SetsCompleted),
                ExternalVolume = entries.Sum(log => log.WeightUsed * log.SetsCompleted * log.RepetitionsCompleted)
            };
        }).ToList();
        var selectedLogs = new List<WorkoutLog>();
        if (selectedDate is not null)
        {
            var selectedLocal = selectedDate.Value.ToDateTime(TimeOnly.MinValue);
            var startUtc = selectedLocal.ToUniversalTime();
            var endUtc = selectedLocal.AddDays(1).ToUniversalTime();
            selectedLogs = await db.WorkoutLogs.AsNoTracking().Include(log => log.WorkoutPlan)
                .Where(log => log.UserProfileId == profile.Id && log.CompletedAt >= startUtc && log.CompletedAt < endUtc)
                .OrderBy(log => log.CompletedAt).ToListAsync(cancellationToken);
        }
        var measurements = await db.ProgressEntries.AsNoTracking().Where(entry => entry.UserProfileId == profile.Id)
            .OrderByDescending(entry => entry.Date).Take(20).ToListAsync(cancellationToken);
        return View(new ProgressCalendarViewModel
        {
            Year = selectedYear, Month = selectedMonth, SelectedDate = selectedDate,
            ActiveDates = activeDates, SelectedLogs = selectedLogs, Measurements = measurements,
            RecentLogs = recentLogs.Take(12).ToList(),
            WorkoutsThisMonth = activityLogs.Where(log => !string.IsNullOrEmpty(log.WorkoutSessionId)).Select(log => log.WorkoutSessionId).Distinct().Count(),
            ActiveDaysThisMonth = activeDates.Count,
            SetsThisMonth = activityLogs.Sum(log => (int)log.SetsCompleted),
            ExternalVolumeThisMonth = activityLogs.Sum(log => log.WeightUsed * log.SetsCompleted * log.RepetitionsCompleted),
            WeeklyActivity = weeklyActivity
        });
    }

    private Task<UserProfile?> CurrentProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return db.UserProfiles.FirstOrDefaultAsync(profile => profile.IdentityUserId == userId, cancellationToken);
    }
}
