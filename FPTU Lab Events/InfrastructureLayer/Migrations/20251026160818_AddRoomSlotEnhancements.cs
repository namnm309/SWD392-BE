using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomSlotEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_StartTime",
                table: "tbl_room_slots");

            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "tbl_room_slots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotNumber",
                table: "tbl_room_slots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "tbl_room_slots",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_room_slots_EventId",
                table: "tbl_room_slots",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_SlotNumber",
                table: "tbl_room_slots",
                columns: new[] { "RoomId", "DayOfWeek", "SlotNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_room_slots_tbl_events_EventId",
                table: "tbl_room_slots",
                column: "EventId",
                principalTable: "tbl_events",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_room_slots_tbl_events_EventId",
                table: "tbl_room_slots");

            migrationBuilder.DropIndex(
                name: "IX_tbl_room_slots_EventId",
                table: "tbl_room_slots");

            migrationBuilder.DropIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_SlotNumber",
                table: "tbl_room_slots");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "tbl_room_slots");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                table: "tbl_room_slots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tbl_room_slots");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_StartTime",
                table: "tbl_room_slots",
                columns: new[] { "RoomId", "DayOfWeek", "StartTime" },
                unique: true);
        }
    }
}
