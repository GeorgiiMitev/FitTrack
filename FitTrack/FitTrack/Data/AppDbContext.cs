using FitTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FitTrack.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<UserProfile> UserProfiles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserProfile>().Property(p => p.Equipment)
                .HasConversion(v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            modelBuilder.Entity<UserProfile>().HasIndex(p => p.IdentityUserId).IsUnique();
            modelBuilder.Entity<WorkoutPlan>().HasMany(p => p.PlanExercises).WithOne(e => e.WorkoutPlan).HasForeignKey(e => e.WorkoutPlanId).OnDelete(DeleteBehavior.Cascade);
        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{

        //    base.OnModelCreating(modelBuilder);

        //    modelBuilder.Entity<UserProfile>()
        //        .HasIndex(u => u.GoogleBookId)
        //        .IsUnique() // не позволява на 2 записа с еднакво id
        //        .HasFilter("[GoogleBookId] IS NOT NULL");
        //}
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutLog> WorkoutLogs { get; set; }
        public DbSet<PlanExercise> PlanExercises { get; set; }
        public DbSet<ProgressEntry> ProgressEntries { get; set; }
    }
}
