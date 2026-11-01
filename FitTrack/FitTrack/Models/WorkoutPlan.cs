using System.ComponentModel.DataAnnotations;

namespace FitTrack.Models
{
    public class WorkoutPlan
    {
        public int Id { get; set; }
        [Required]
        public int UserProfileId { get; set; }   
        public UserProfile? UserProfile { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Name { get; set; } = "Workout plan";
        public string Goal {  get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsAiGenerated { get; set; }
        public ICollection<PlanExercise> PlanExercises { get; set; } = new List<PlanExercise>();

    }
}
