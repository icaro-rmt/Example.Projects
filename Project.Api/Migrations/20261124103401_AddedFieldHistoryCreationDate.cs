using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedFieldHistoryCreationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ValidationRequests_History",
                table: "ValidationRequests_History");

            migrationBuilder.RenameTable(
                name: "ValidationRequests_History",
                newName: "ValidationRequest_History");

            migrationBuilder.RenameIndex(
                name: "IX_ValidationRequests_History_ValidationRequestId_HistoryId",
                table: "ValidationRequest_History",
                newName: "IX_ValidationRequest_History_ValidationRequestId_HistoryId");

            migrationBuilder.AddColumn<DateTime>(
                name: "HistoryCreationDate",
                table: "Process_History",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "HistoryCreationDate",
                table: "ValidationRequest_History",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValidationRequest_History",
                table: "ValidationRequest_History",
                column: "HistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ValidationRequest_History",
                table: "ValidationRequest_History");

            migrationBuilder.DropColumn(
                name: "HistoryCreationDate",
                table: "Process_History");

            migrationBuilder.DropColumn(
                name: "HistoryCreationDate",
                table: "ValidationRequest_History");

            migrationBuilder.RenameTable(
                name: "ValidationRequest_History",
                newName: "ValidationRequests_History");

            migrationBuilder.RenameIndex(
                name: "IX_ValidationRequest_History_ValidationRequestId_HistoryId",
                table: "ValidationRequests_History",
                newName: "IX_ValidationRequests_History_ValidationRequestId_HistoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValidationRequests_History",
                table: "ValidationRequests_History",
                column: "HistoryId");
        }
    }
}
