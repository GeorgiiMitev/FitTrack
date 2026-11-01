using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Migrations
{
    /// <inheritdoc />
    public partial class CoreFitnessFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_UserProfileId",
                table: "WorkoutLogs");

            migrationBuilder.AddColumn<bool>(
                name: "IsAiGenerated",
                table: "WorkoutPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WorkoutPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "PlanExerciseId",
                table: "WorkoutLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutPlanId",
                table: "WorkoutLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkoutSessionId",
                table: "WorkoutLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExperienceLevel",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredTrainingDays",
                table: "UserProfiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePhotoPath",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SetupCompleted",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WgerExerciseId",
                table: "PlanExercises",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_PlanExerciseId",
                table: "WorkoutLogs",
                column: "PlanExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_UserProfileId_CompletedAt",
                table: "WorkoutLogs",
                columns: new[] { "UserProfileId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_WorkoutPlanId",
                table: "WorkoutLogs",
                column: "WorkoutPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_PlanExercises_PlanExerciseId",
                table: "WorkoutLogs",
                column: "PlanExerciseId",
                principalTable: "PlanExercises",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutLogs_WorkoutPlans_WorkoutPlanId",
                table: "WorkoutLogs",
                column: "WorkoutPlanId",
                principalTable: "WorkoutPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_PlanExercises_PlanExerciseId",
                table: "WorkoutLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutLogs_WorkoutPlans_WorkoutPlanId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_PlanExerciseId",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_UserProfileId_CompletedAt",
                table: "WorkoutLogs");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutLogs_WorkoutPlanId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "IsAiGenerated",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "PlanExerciseId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "WorkoutSessionId",
                table: "WorkoutLogs");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredTrainingDays",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoPath",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "SetupCompleted",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WgerExerciseId",
                table: "PlanExercises");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutLogs_UserProfileId",
                table: "WorkoutLogs",
                column: "UserProfileId");
        }
    }
}
