using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "tbl_events");

            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "tbl_events",
                newName: "CreatedBy");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "tbl_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "tbl_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "tbl_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "tbl_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_events_CreatedBy",
                table: "tbl_events",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_events_tbl_users_CreatedBy",
                table: "tbl_events",
                column: "CreatedBy",
                principalTable: "tbl_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_events_tbl_users_CreatedBy",
                table: "tbl_events");

            migrationBuilder.DropIndex(
                name: "IX_tbl_events_CreatedBy",
                table: "tbl_events");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "tbl_events");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "tbl_events");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "tbl_events");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "tbl_events");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "tbl_events",
                newName: "OrganizationId");

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "tbl_events",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
