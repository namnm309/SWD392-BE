using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddLabRoomRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoomId",
                table: "tbl_labs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_labs_RoomId",
                table: "tbl_labs",
                column: "RoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_labs_tbl_rooms_RoomId",
                table: "tbl_labs",
                column: "RoomId",
                principalTable: "tbl_rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_labs_tbl_rooms_RoomId",
                table: "tbl_labs");

            migrationBuilder.DropIndex(
                name: "IX_tbl_labs_RoomId",
                table: "tbl_labs");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "tbl_labs");
        }
    }
}
