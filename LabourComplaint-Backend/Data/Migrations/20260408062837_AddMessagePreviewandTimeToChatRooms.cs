using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabourComplaint_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagePreviewandTimeToChatRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAt",
                table: "ChatRooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessagePreview",
                table: "ChatRooms",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMessageAt",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "LastMessagePreview",
                table: "ChatRooms");
        }
    }
}
