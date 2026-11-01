using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FitTrack.Models;

public class OnboardingViewModel
{
    [Required, StringLength(80)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Goal { get; set; } = "General fitness";
    public FitnessExperienceLevel ExperienceLevel { get; set; } = FitnessExperienceLevel.Beginner;
    [Range(15, 240)] public int SessionDurationMinutes { get; set; } = 45;
    [MinLength(1, ErrorMessage = "Choose at least one training day.")] public List<string> PreferredTrainingDays { get; set; } = new();
    [MinLength(1, ErrorMessage = "Choose at least one equipment option.")] public List<string> Equipment { get; set; } = new();
}

public sealed class ProfileEditViewModel : OnboardingViewModel
{
    public string? CurrentPhotoPath { get; set; }
    public IFormFile? ProfilePhoto { get; set; }
}

public sealed class ExerciseLibraryViewModel
{
    public string? Search { get; set; }
    public string? Muscle { get; set; }
    public string? Category { get; set; }
    public string? Equipment { get; set; }
    public string? Error { get; set; }
    public bool UsingCachedData { get; set; }
    public List<string> Muscles { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> EquipmentOptions { get; set; } = new();
    public List<ExerciseSummary> Exercises { get; set; } = new();
}

public sealed class ManualPlanViewModel
{
    [Required, StringLength(80)] public string PlanName { get; set; } = "My workout plan";
    [Required, StringLength(80)] public string Goal { get; set; } = string.Empty;
    [MinLength(1, ErrorMessage = "Select at least one exercise.")] public List<int> ExerciseIds { get; set; } = new();
    public string? Error { get; set; }
    public List<ExerciseSummary> AvailableExercises { get; set; } = new();
}

public sealed class WorkoutCompletionViewModel
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public List<WorkoutCompletionExercise> Exercises { get; set; } = new();
}

public sealed class WorkoutCompletionExercise
{
    public int PlanExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public byte PrescribedSets { get; set; }
    public byte PrescribedRepetitions { get; set; }
    public byte SetsCompleted { get; set; }
    public byte RepetitionsCompleted { get; set; }
    public decimal WeightUsed { get; set; }
    public bool Completed { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class ProgressCalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly? SelectedDate { get; set; }
    public HashSet<DateOnly> ActiveDates { get; set; } = new();
    public List<WorkoutLog> SelectedLogs { get; set; } = new();
    public List<ProgressEntry> Measurements { get; set; } = new();
    public List<WorkoutLog> RecentLogs { get; set; } = new();
    public int WorkoutsThisMonth { get; set; }
    public int ActiveDaysThisMonth { get; set; }
    public int SetsThisMonth { get; set; }
    public decimal ExternalVolumeThisMonth { get; set; }
    public List<WeeklyProgressPoint> WeeklyActivity { get; set; } = new();
}

public sealed class WeeklyProgressPoint
{
    public DateOnly WeekStarting { get; set; }
    public int Workouts { get; set; }
    public int ActiveDays { get; set; }
    public int Sets { get; set; }
    public decimal ExternalVolume { get; set; }
}

public sealed class DashboardViewModel
{
    public UserProfile Profile { get; set; } = new();
    public WorkoutPlan? ActivePlan { get; set; }
    public List<WorkoutLog> RecentWorkouts { get; set; } = new();
    public int CompletedWorkoutsThisWeek { get; set; }
    public int ActiveDaysThisMonth { get; set; }
}

public sealed class ManualWorkoutLogViewModel
{
    [Required, StringLength(100)] public string ExerciseName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.Now;
    [Range(1, 20)] public byte SetsCompleted { get; set; } = 3;
    [Range(1, 100)] public byte RepetitionsCompleted { get; set; } = 10;
    [Range(typeof(decimal), "0", "1000")] public decimal WeightUsed { get; set; }
    [StringLength(500)] public string Notes { get; set; } = string.Empty;
}
