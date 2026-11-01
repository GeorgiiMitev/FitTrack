using System.ComponentModel.DataAnnotations;
namespace FitTrack.Models;

public class UserProfile
{

    //[MaxLength(100)]
    //public string? GoogleBookId { get; set; }
    public int Id { get; set; }
    [Required]
    public string IdentityUserId { get; set; } = string.Empty;
    public ApplicationUser? IdentityUser { get; set; }
    public FitnessExperienceLevel ExperienceLevel { get; set; } = FitnessExperienceLevel.Beginner;
    public bool SetupCompleted { get; set; }
    [StringLength(300)] public string? ProfilePhotoPath { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(500)]
    public string Goal { get; set; } = string.Empty;
    [Range(1, 7)]
    public byte TrainingDaysPerWeek { get; set; }
    [Range(10, 300)]
    public byte SessionDurationMinutes { get; set; }

    public List<string> Equipment { get; set; } = new();
    public List<string> PreferredTrainingDays { get; set; } = new();

    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
    public ICollection<WorkoutLog> WorkoutLogs { get; set; } = new List<WorkoutLog>();
    public ICollection<ProgressEntry> ProgressEntries { get; set; } = new List<ProgressEntry>();

}

public enum FitnessExperienceLevel
{
    Beginner,
    Intermediate,
    Advanced
}

