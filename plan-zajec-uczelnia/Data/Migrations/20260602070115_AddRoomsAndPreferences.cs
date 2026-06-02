using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace plan_zajec_uczelnia.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomsAndPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredRoomNumber",
                table: "Subjects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    RoomNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.RoomNumber);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Rooms_PreferredRoomNumber",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "RoomAvailabilities");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_PreferredRoomNumber",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "PreferredRoomNumber",
                table: "Subjects");
        }
    }
}
