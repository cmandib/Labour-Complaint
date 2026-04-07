using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabourComplaint_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictIdToOutboxMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "OutboxMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OutboxMessages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OutboxMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DistrictId_EventType",
                table: "OutboxMessages",
                columns: new[] { "DistrictId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt_RetryCount",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_DistrictId_EventType",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAt_RetryCount",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OutboxMessages");
        }
    }
}
