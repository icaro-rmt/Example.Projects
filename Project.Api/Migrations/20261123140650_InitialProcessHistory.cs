using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Project.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialProcess_History : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ProcessIds",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "Processes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ProcessTypeId = table.Column<int>(type: "integer", nullable: false),
                    ProcessStatusId = table.Column<int>(type: "integer", nullable: false),
                    ClientNIF = table.Column<string>(type: "text", nullable: false),
                    ClientName = table.Column<string>(type: "text", nullable: false),
                    ClientSector = table.Column<string>(type: "text", nullable: false),
                    ClientCountryCode = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreationUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdateUser = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Processes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Process_History",
                columns: table => new
                {
                    HistoryProcessId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcessId = table.Column<int>(type: "integer", nullable: false),
                    ProcessTypeId = table.Column<int>(type: "integer", nullable: false),
                    ProcessStatusId = table.Column<int>(type: "integer", nullable: false),
                    ClientNIF = table.Column<string>(type: "text", nullable: false),
                    ClientName = table.Column<string>(type: "text", nullable: false),
                    ClientSector = table.Column<string>(type: "text", nullable: false),
                    ClientCountryCode = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreationUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdateUser = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Process_History", x => x.HistoryProcessId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Process_History_ProcessId_HistoryProcessId",
                table: "Process_History",
                columns: new[] { "ProcessId", "HistoryProcessId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Processes");

            migrationBuilder.DropTable(
                name: "Process_History");

            migrationBuilder.DropSequence(
                name: "ProcessIds");
        }
    }
}
