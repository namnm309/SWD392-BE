using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoleStaticTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastUpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastUpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastUpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "LastUpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(112), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(272) });

            migrationBuilder.UpdateData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "LastUpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(398), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(399) });

            migrationBuilder.UpdateData(
                table: "tbl_roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "LastUpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(401), new DateTime(2025, 9, 22, 15, 17, 20, 324, DateTimeKind.Utc).AddTicks(401) });
        }
    }
}
