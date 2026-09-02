using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace plan_zajec_uczelnia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNonWorkingDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NonWorkingDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonWorkingDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonWorkingDays_AcademicYear_Date",
                table: "NonWorkingDays",
                columns: new[] { "AcademicYear", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NonWorkingDays");
        }
    }
}
