using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabourComplaint_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictIdToChatRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "ChatRooms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_DistrictId",
                table: "ChatRooms",
                column: "DistrictId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_Districts_DistrictId",
                table: "ChatRooms",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_Districts_DistrictId",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_DistrictId",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "ChatRooms");
        }
    }
}
