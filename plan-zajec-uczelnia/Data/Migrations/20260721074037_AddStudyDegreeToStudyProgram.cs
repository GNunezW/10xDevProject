using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace plan_zajec_uczelnia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyDegreeToStudyProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudyDegree",
                table: "StudyPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudyDegree",
                table: "StudyPrograms");
        }
    }
}
