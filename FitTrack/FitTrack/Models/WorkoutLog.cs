namespace FitTrack.Models
{
    public class WorkoutLog
    {
        public int Id { get; set; }
        public string ExerciseName {  get; set; }
        public int UserProfileId { get; set; }
        public UserProfile? UserProfile { get; set; }
        public DateTime CompletedAt { get; set; }
        public byte SetsCompleted { get; set; }
        public byte RepetitionsCompleted { get; set; }
        public decimal WeightUsed { get; set; } 
        public string Notes { get; set; }


    }
}
