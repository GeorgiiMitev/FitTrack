using FitTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<PlanExercise> PlanExercises { get; set; }
        public DbSet<ProgressEntry> ProgressEntries { get; set; }
    }
}
