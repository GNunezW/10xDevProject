using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace plan_zajec_uczelnia.Data.Migrations
{
    /// <inheritdoc />
    public partial class InstructionTypesConfigurable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchedulingSettings");

            migrationBuilder.RenameColumn(
                name: "LessonType",
                table: "Subjects",
                newName: "InstructionTypeId");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Rooms",
                newName: "InstructionTypeId");

            migrationBuilder.CreateTable(
                name: "InstructionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxStudentsPerGroup = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructionTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "InstructionTypes",
                columns: new[] { "Id", "MaxStudentsPerGroup", "Name" },
                values: new object[,]
                {
                    { 1, 120, "Wykład" },
                    { 2, 30, "Ćwiczenia" },
                    { 3, 24, "Laboratorium" }
                });

            migrationBuilder.Sql(
                """
                UPDATE "Subjects" SET "InstructionTypeId" = 1
                WHERE "InstructionTypeId" NOT IN (1, 2, 3);
                UPDATE "Rooms" SET "InstructionTypeId" = 1
                WHERE "InstructionTypeId" NOT IN (1, 2, 3);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_InstructionTypeId",
                table: "Subjects",
                column: "InstructionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_InstructionTypeId",
                table: "Rooms",
                column: "InstructionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructionTypes_Name",
                table: "InstructionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_InstructionTypes_InstructionTypeId",
                table: "Rooms",
                column: "InstructionTypeId",
                principalTable: "InstructionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_InstructionTypes_InstructionTypeId",
                table: "Subjects",
                column: "InstructionTypeId",
                principalTable: "InstructionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_InstructionTypes_InstructionTypeId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_InstructionTypes_InstructionTypeId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "InstructionTypes");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_InstructionTypeId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_InstructionTypeId",
                table: "Rooms");

            migrationBuilder.RenameColumn(
                name: "InstructionTypeId",
                table: "Subjects",
                newName: "LessonType");

            migrationBuilder.RenameColumn(
                name: "InstructionTypeId",
                table: "Rooms",
                newName: "Type");

            migrationBuilder.CreateTable(
                name: "SchedulingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxStudentsExercise = table.Column<int>(type: "integer", nullable: false),
                    MaxStudentsLaboratory = table.Column<int>(type: "integer", nullable: false),
                    MaxStudentsLecture = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SchedulingSettings",
                columns: new[] { "Id", "MaxStudentsExercise", "MaxStudentsLaboratory", "MaxStudentsLecture" },
                values: new object[] { 1, 30, 24, 120 });
        }
    }
}
