using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class TuAddEquipmentNotificationReportRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TargetGroup = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_notifications_tbl_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ReportedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminResponse = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_reports_tbl_users_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tbl_reports_tbl_users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_notification_reads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_notification_reads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_notification_reads_tbl_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "tbl_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_notification_reads_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_bookings_tbl_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "tbl_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_bookings_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tbl_equipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SerialNumber = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_equipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_equipments_tbl_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "tbl_rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_bookings_RoomId_StartTime_EndTime_Status",
                table: "tbl_bookings",
                columns: new[] { "RoomId", "StartTime", "EndTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_bookings_UserId",
                table: "tbl_bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_equipments_RoomId",
                table: "tbl_equipments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_equipments_SerialNumber",
                table: "tbl_equipments",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_equipments_Type_Status_RoomId",
                table: "tbl_equipments",
                columns: new[] { "Type", "Status", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notification_reads_NotificationId",
                table: "tbl_notification_reads",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notification_reads_UserId",
                table: "tbl_notification_reads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notifications_CreatedBy",
                table: "tbl_notifications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_notifications_TargetGroup_Status_StartDate_EndDate",
                table: "tbl_notifications",
                columns: new[] { "TargetGroup", "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reports_ReporterId",
                table: "tbl_reports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reports_ResolvedBy",
                table: "tbl_reports",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_reports_Type_Status_ReportedDate",
                table: "tbl_reports",
                columns: new[] { "Type", "Status", "ReportedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rooms_Name_Location_Status",
                table: "tbl_rooms",
                columns: new[] { "Name", "Location", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_bookings");

            migrationBuilder.DropTable(
                name: "tbl_equipments");

            migrationBuilder.DropTable(
                name: "tbl_notification_reads");

            migrationBuilder.DropTable(
                name: "tbl_reports");

            migrationBuilder.DropTable(
                name: "tbl_rooms");

            migrationBuilder.DropTable(
                name: "tbl_notifications");
        }
    }
}
