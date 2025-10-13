using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddLabLabMemberEventRoomSlotBookingApply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "tbl_bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Visibility = table.Column<bool>(type: "boolean", nullable: false),
                    RecurrenceRule = table.Column<string>(type: "text", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_labs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_labs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_room_slots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_room_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_room_slots_tbl_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "tbl_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_lab_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_lab_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_lab_members_tbl_labs_LabId",
                        column: x => x.LabId,
                        principalTable: "tbl_labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_lab_members_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_booking_applies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_booking_applies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_booking_applies_tbl_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "tbl_bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_booking_applies_tbl_room_slots_RoomSlotId",
                        column: x => x.RoomSlotId,
                        principalTable: "tbl_room_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_bookings_EventId",
                table: "tbl_bookings",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_applies_BookingId",
                table: "tbl_booking_applies",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_booking_applies_RoomSlotId",
                table: "tbl_booking_applies",
                column: "RoomSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_lab_members_LabId_UserId",
                table: "tbl_lab_members",
                columns: new[] { "LabId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_lab_members_UserId",
                table: "tbl_lab_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_room_slots_RoomId_DayOfWeek_StartTime",
                table: "tbl_room_slots",
                columns: new[] { "RoomId", "DayOfWeek", "StartTime" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_bookings_tbl_events_EventId",
                table: "tbl_bookings",
                column: "EventId",
                principalTable: "tbl_events",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_bookings_tbl_events_EventId",
                table: "tbl_bookings");

            migrationBuilder.DropTable(
                name: "tbl_booking_applies");

            migrationBuilder.DropTable(
                name: "tbl_events");

            migrationBuilder.DropTable(
                name: "tbl_lab_members");

            migrationBuilder.DropTable(
                name: "tbl_room_slots");

            migrationBuilder.DropTable(
                name: "tbl_labs");

            migrationBuilder.DropIndex(
                name: "IX_tbl_bookings_EventId",
                table: "tbl_bookings");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "tbl_bookings");
        }
    }
}
