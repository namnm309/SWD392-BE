using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddDateToRoomSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_SlotNumber",
                table: "tbl_room_slots");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "tbl_room_slots",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_tbl_room_slots_RoomId_Date_SlotNumber",
                table: "tbl_room_slots",
                columns: new[] { "RoomId", "Date", "SlotNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_room_slots_RoomId_Date_SlotNumber",
                table: "tbl_room_slots");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "tbl_room_slots");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_SlotNumber",
                table: "tbl_room_slots",
                columns: new[] { "RoomId", "DayOfWeek", "SlotNumber" },
                unique: true);
        }
    }
}
