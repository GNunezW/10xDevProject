using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace plan_zajec_uczelnia.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorSchedulingCapacityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Rooms_PreferredRoomNumber",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "RoomAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_PreferredRoomNumber",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "PreferredRoomNumber",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Rooms");

            migrationBuilder.AddColumn<int>(
                name: "LessonType",
                table: "Subjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SchedulingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxStudentsLecture = table.Column<int>(type: "integer", nullable: false),
                    MaxStudentsExercise = table.Column<int>(type: "integer", nullable: false),
                    MaxStudentsLaboratory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyProgramEnrollments",
                columns: table => new
                {
                    StudyProgramId = table.Column<int>(type: "integer", nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    StudentCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyProgramEnrollments", x => new { x.StudyProgramId, x.Semester });
                    table.ForeignKey(
                        name: "FK_StudyProgramEnrollments_StudyPrograms_StudyProgramId",
                        column: x => x.StudyProgramId,
                        principalTable: "StudyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SchedulingSettings",
                columns: new[] { "Id", "MaxStudentsExercise", "MaxStudentsLaboratory", "MaxStudentsLecture" },
                values: new object[] { 1, 30, 24, 120 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchedulingSettings");

            migrationBuilder.DropTable(
                name: "StudyProgramEnrollments");

            migrationBuilder.DropColumn(
                name: "LessonType",
                table: "Subjects");

            migrationBuilder.AddColumn<string>(
                name: "PreferredRoomNumber",
                table: "Subjects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Rooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoomAvailabilities",
                columns: table => new
                {
                    RoomNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAvailabilities", x => new { x.RoomNumber, x.DayOfWeek, x.TimeSlotId });
                    table.ForeignKey(
                        name: "FK_RoomAvailabilities_Rooms_RoomNumber",
                        column: x => x.RoomNumber,
                        principalTable: "Rooms",
                        principalColumn: "RoomNumber",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomAvailabilities_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_PreferredRoomNumber",
                table: "Subjects",
                column: "PreferredRoomNumber");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAvailabilities_TimeSlotId",
                table: "RoomAvailabilities",
                column: "TimeSlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Rooms_PreferredRoomNumber",
                table: "Subjects",
                column: "PreferredRoomNumber",
                principalTable: "Rooms",
                principalColumn: "RoomNumber",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
