namespace FitTrack.Models
{
    public class Equipment
    {
        public static List<string> GetAllEquipment()
        {
            return new List<string>
            {
                // Free Weights
                "Dumbbells",
                "Barbell",
                "EZ Curl Bar",
                "Kettlebells",
                "Weight Plates",
                "Medicine Ball",
                "Trap Bar",

                // Machines
                "Smith Machine",
                "Cable Machine",
                "Leg Press Machine",
                "Lat Pulldown Machine",
                "Chest Press Machine",
                "Leg Extension Machine",
                "Leg Curl Machine",
                "Pec Deck Machine",
                "Assisted Pull-Up Machine",

                // Bodyweight Equipment
                "Pull-Up Bar",
                "Dip Bars",
                "Parallel Bars",
                "Gymnastic Rings",
                "Push-Up Bars",

                // Resistance Equipment
                "Resistance Bands",
                "Resistance Tubes",
                "Suspension Trainer",
                "Weighted Vest",

                // Benches and Racks
                "Flat Bench",
                "Adjustable Bench",
                "Squat Rack",
                "Power Rack",
                "Preacher Curl Bench",

                // Cardio Equipment
                "Treadmill",
                "Exercise Bike",
                "Rowing Machine",
                "Elliptical Machine",

                // Accessories
                "Jump Rope",
                "Ab Wheel",
                "Exercise Mat",
                "Foam Roller"
            };
        }


    }
}
