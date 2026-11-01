using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitTrack.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePreferredTrainingDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredTrainingDays",
                table: "UserProfiles",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "'[]'",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.Sql("UPDATE UserProfiles SET PreferredTrainingDays = '[]' WHERE PreferredTrainingDays = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredTrainingDays",
                table: "UserProfiles",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValueSql: "'[]'");
        }
    }
}
