using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Migrations;

public partial class AddExerciseNameToProgressEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExerciseName",
            table: "ProgressEntries",
            type: "TEXT",
            maxLength: 100,
            nullable: false,
            defaultValue: "General exercise");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ExerciseName", table: "ProgressEntries");
    }
}
