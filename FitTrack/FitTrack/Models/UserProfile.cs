namespace FitTrack.Models

{
    public class UserProfile
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public string Goal {  get; set; }
        public enum ExperienceLevel
        {
            Beginner,
            Intermediate,
            Advanced,
            Professional
        }

        public byte TrainingDaysPerWeek { get; set; }
        public byte SessionDurationMinutes { get; set; }

        public List<string> Equipment { get; set; }

        public ICollection<WorkoutPlan> WorkoutPlans { get; set; }
        public ICollection<WorkoutLog> WorkoutLogs { get; set; }
        public ICollection<ProgressEntry> ProgressEntries { get; set; }

    }
}
