using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSessionAndSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_user_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Device = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_user_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_user_sessions_tbl_users_UserId",
                        column: x => x.UserId,
                        principalTable: "tbl_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tbl_roles",
                columns: new[] { "Id", "CreatedAt", "LastUpdatedAt", "description", "name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(112), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(272), "System administrator", "Admin" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(398), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(399), "Lecturer", "Lecturer" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(401), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(401), "Student", "Student" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_users_Email",
                table: "tbl_users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_users_Username",
                table: "tbl_users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_user_sessions_UserId",
                table: "tbl_user_sessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_user_sessions");

            migrationBuilder.DropIndex(
                name: "IX_tbl_users_Email",
                table: "tbl_users");

            migrationBuilder.DropIndex(
                name: "IX_tbl_users_Username",
                table: "tbl_users");

            migrationBuilder.DeleteData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));
        }
    }
}
