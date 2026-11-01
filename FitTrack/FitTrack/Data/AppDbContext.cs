using FitTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FitTrack.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }


        public DbSet<UserProfile> UserProfiles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var listConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
                value => System.Text.Json.JsonSerializer.Serialize(value, (System.Text.Json.JsonSerializerOptions?)null),
                value => System.Text.Json.JsonSerializer.Deserialize<List<string>>(value, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
            var listComparer = new ValueComparer<List<string>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList());
            modelBuilder.Entity<UserProfile>().Property(p => p.Equipment).HasConversion(listConverter).Metadata.SetValueComparer(listComparer);
            modelBuilder.Entity<UserProfile>().Property(p => p.PreferredTrainingDays).HasConversion(listConverter).HasDefaultValueSql("'[]'").Metadata.SetValueComparer(listComparer);
            modelBuilder.Entity<UserProfile>().HasIndex(p => p.IdentityUserId).IsUnique();
            modelBuilder.Entity<WorkoutPlan>().HasMany(p => p.PlanExercises).WithOne(e => e.WorkoutPlan).HasForeignKey(e => e.WorkoutPlanId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkoutLog>().HasOne(log => log.WorkoutPlan).WithMany().HasForeignKey(log => log.WorkoutPlanId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<WorkoutLog>().HasOne(log => log.PlanExercise).WithMany().HasForeignKey(log => log.PlanExerciseId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<WorkoutLog>().HasIndex(log => new { log.UserProfileId, log.CompletedAt });
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
