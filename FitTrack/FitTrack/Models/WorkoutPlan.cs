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
        public string Goal {  get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<PlanExercise> PlanExercises { get; set; }

    }
}
