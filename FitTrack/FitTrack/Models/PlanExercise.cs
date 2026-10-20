namespace FitTrack.Models
{
    public class PlanExercise
    {
        public int Id { get; set; }
        public string ExerciseName {  get; set; }
        public int WorkoutPlanId { get; set; }
        public WorkoutPlan? WorkoutPlan {  get; set; }
        public byte DayOfTheWeek { get; set; }
        public byte Sets { get; set; }
        public byte Repetitions { get; set; }
        public byte RestSeconds { get; set; }


    }
}
