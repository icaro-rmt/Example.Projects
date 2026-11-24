using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Project.Api.Migrations
{
    /// <inheritdoc />
    public partial class ValidationRequestModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ValidationRequestId",
                incrementBy: 10);

            migrationBuilder.CreateTable(
                name: "ValidationRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DocumentId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreationUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdateUser = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationRequests_History",
                columns: table => new
                {
                    HistoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ValidationRequestId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentId = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreationUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdateUser = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRequests_History", x => x.HistoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequests_History_ValidationRequestId_HistoryId",
                table: "ValidationRequests_History",
                columns: new[] { "ValidationRequestId", "HistoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationRequests");

            migrationBuilder.DropTable(
                name: "ValidationRequests_History");

            migrationBuilder.DropSequence(
                name: "ValidationRequestId");
        }
    }
}
