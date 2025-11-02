using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDescriptionLocationImageUrlFromRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_rooms_Name_Location_Status",
                table: "tbl_rooms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "tbl_rooms");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "tbl_rooms");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "tbl_rooms");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rooms_Name_Status",
                table: "tbl_rooms",
                columns: new[] { "Name", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_rooms_Name_Status",
                table: "tbl_rooms");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "tbl_rooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "tbl_rooms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "tbl_rooms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_rooms_Name_Location_Status",
                table: "tbl_rooms",
                columns: new[] { "Name", "Location", "Status" });
        }
    }
}
