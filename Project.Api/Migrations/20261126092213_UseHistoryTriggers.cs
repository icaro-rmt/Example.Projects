using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Api.Migrations
{
    /// <inheritdoc />
    public partial class UseHistoryTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ValidationRequest_History",
                table: "ValidationRequest_History");

            migrationBuilder.DropIndex(
                name: "IX_ValidationRequest_History_ValidationRequestId_HistoryId",
                table: "ValidationRequest_History");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Process_History",
                table: "Process_History");

            migrationBuilder.DropIndex(
                name: "IX_Process_History_ProcessId_HistoryProcessId",
                table: "Process_History");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HistoryCreationDate",
                table: "ValidationRequest_History",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HistoryCreationDate",
                table: "Process_History",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "statement_timestamp()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequest_History_ValidationRequestId_HistoryCreati~",
                table: "ValidationRequest_History",
                columns: new[] { "ValidationRequestId", "HistoryCreationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Process_History_ProcessId_HistoryCreationDate",
                table: "Process_History",
                columns: new[] { "ProcessId", "HistoryCreationDate" });

            CreateHistoryTrigger(migrationBuilder, "Processes", "Process",
                "ProcessTypeId", "ProcessStatusId", "ClientNIF", "ClientName", "ClientSector",
                "ClientCountryCode", "CreationDate", "UpdateDate", "CreationUserId", "UpdateUser", "Comments");
            CreateHistoryTrigger(migrationBuilder, "ValidationRequests", "ValidationRequest",
                "DocumentId", "CreationDate", "UpdateDate", "CreationUserId", "UpdateUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            DropHistoryTrigger(migrationBuilder, "Processes", "Process");
            DropHistoryTrigger(migrationBuilder, "ValidationRequests", "ValidationRequest");

            migrationBuilder.DropIndex(
                name: "IX_ValidationRequest_History_ValidationRequestId_HistoryCreati~",
                table: "ValidationRequest_History");

            migrationBuilder.DropIndex(
                name: "IX_Process_History_ProcessId_HistoryCreationDate",
                table: "Process_History");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HistoryCreationDate",
                table: "ValidationRequest_History",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "statement_timestamp()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HistoryCreationDate",
                table: "Process_History",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "statement_timestamp()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ValidationRequest_History",
                table: "ValidationRequest_History",
                column: "HistoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Process_History",
                table: "Process_History",
                column: "HistoryProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequest_History_ValidationRequestId_HistoryId",
                table: "ValidationRequest_History",
                columns: new[] { "ValidationRequestId", "HistoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Process_History_ProcessId_HistoryProcessId",
                table: "Process_History",
                columns: new[] { "ProcessId", "HistoryProcessId" });
        }

        private static void CreateHistoryTrigger(
            MigrationBuilder migrationBuilder, string sourceTable, string entityName, params string[] sourceColumns)
        {
            var columns = string.Join(", ", sourceColumns.Select(column => $@"""{column}"""));
            var values = string.Join(", ", sourceColumns.Select(column => $@"snapshot.""{column}"""));

            migrationBuilder.Sql($"""
                CREATE FUNCTION "Capture{entityName}History"() RETURNS trigger
                LANGUAGE plpgsql AS $history$
                DECLARE
                    snapshot "{sourceTable}"%ROWTYPE;
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        snapshot := OLD;
                    ELSE
                        snapshot := NEW;
                    END IF;

                    INSERT INTO "{entityName}_History" ("{entityName}Id", {columns})
                    VALUES (snapshot."Id", {values});
                    RETURN NULL;
                END;
                $history$;

                CREATE TRIGGER "CaptureHistory"
                AFTER INSERT OR UPDATE OR DELETE ON "{sourceTable}"
                FOR EACH ROW EXECUTE FUNCTION "Capture{entityName}History"();
                """);
        }

        private static void DropHistoryTrigger(
            MigrationBuilder migrationBuilder, string sourceTable, string entityName)
        {
            migrationBuilder.Sql($"""
                DROP TRIGGER "CaptureHistory" ON "{sourceTable}";
                DROP FUNCTION "Capture{entityName}History"();
                """);
        }
    }
}
