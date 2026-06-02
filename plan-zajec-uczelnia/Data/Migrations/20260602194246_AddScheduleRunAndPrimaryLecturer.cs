using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace plan_zajec_uczelnia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleRunAndPrimaryLecturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "SubjectLecturers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScheduleRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SolverTimeLimitSeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScheduleRunId = table.Column<int>(type: "integer", nullable: false),
                    StudyProgramId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    GroupIndex = table.Column<int>(type: "integer", nullable: false),
                    SessionIndex = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    LecturerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_Lecturers_LecturerId",
                        column: x => x.LecturerId,
                        principalTable: "Lecturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_Rooms_RoomNumber",
                        column: x => x.RoomNumber,
                        principalTable: "Rooms",
                        principalColumn: "RoomNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_ScheduleRuns_ScheduleRunId",
                        column: x => x.ScheduleRunId,
                        principalTable: "ScheduleRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_StudyPrograms_StudyProgramId",
                        column: x => x.StudyProgramId,
                        principalTable: "StudyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledSessions_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_LecturerId",
                table: "ScheduledSessions",
                column: "LecturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_RoomNumber",
                table: "ScheduledSessions",
                column: "RoomNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_ScheduleRunId_SubjectId_GroupIndex_Sessio~",
                table: "ScheduledSessions",
                columns: new[] { "ScheduleRunId", "SubjectId", "GroupIndex", "SessionIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_StudyProgramId",
                table: "ScheduledSessions",
                column: "StudyProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_SubjectId",
                table: "ScheduledSessions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledSessions_TimeSlotId",
                table: "ScheduledSessions",
                column: "TimeSlotId");

            migrationBuilder.Sql("""
                UPDATE "SubjectLecturers" sl
                SET "IsPrimary" = true
                FROM (
                    SELECT "SubjectId", MIN("LecturerId") AS "MinLecturerId"
                    FROM "SubjectLecturers"
                    GROUP BY "SubjectId"
                ) fp
                WHERE sl."SubjectId" = fp."SubjectId"
                  AND sl."LecturerId" = fp."MinLecturerId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledSessions");

            migrationBuilder.DropTable(
                name: "ScheduleRuns");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "SubjectLecturers");
        }
    }
}
