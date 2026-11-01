namespace FitTrack.Models
{
    public class WorkoutLog
    {
        public int Id { get; set; }
        public string ExerciseName {  get; set; } = string.Empty;
        public int UserProfileId { get; set; }
        public UserProfile? UserProfile { get; set; }
        public int? WorkoutPlanId { get; set; }
        public WorkoutPlan? WorkoutPlan { get; set; }
        public int? PlanExerciseId { get; set; }
        public PlanExercise? PlanExercise { get; set; }
        public string WorkoutSessionId { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public byte SetsCompleted { get; set; }
        public byte RepetitionsCompleted { get; set; }
        public decimal WeightUsed { get; set; } 
        public string Notes { get; set; } = string.Empty;


    }
}
