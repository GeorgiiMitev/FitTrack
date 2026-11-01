namespace FitTrack.Models
{
    public class ProgressEntry
    {
        public int Id { get; set; }
        public int UserProfileId {  get; set; }
        public UserProfile? UserProfile { get; set; }
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]
        public string ExerciseName { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.1", "1000")]
        public decimal MaxWeight { get; set; }
        public DateOnly Date { get; set; }

    }
}
