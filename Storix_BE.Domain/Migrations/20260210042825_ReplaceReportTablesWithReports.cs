using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceReportTablesWithReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    report_type = table.Column<string>(type: "character varying", nullable: true),
                    warehouse_id = table.Column<int>(type: "integer", nullable: true),
                    time_from = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    time_to = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    parameters_json = table.Column<string>(type: "jsonb", nullable: true),
                    summary_json = table.Column<string>(type: "jsonb", nullable: true),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    schema_version = table.Column<string>(type: "character varying", nullable: true),
                    pdf_url = table.Column<string>(type: "text", nullable: true),
                    pdf_file_name = table.Column<string>(type: "character varying", nullable: true),
                    pdf_content_hash = table.Column<string>(type: "character varying", nullable: true),
                    pdf_generated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reports_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_reports_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_reports_company_id",
                table: "reports",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_created_by_user_id",
                table: "reports",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_warehouse_id",
                table: "reports",
                column: "warehouse_id");

            // Best-effort data migration from old tables (if they exist).
            // Note: artifacts are not migrated (single-table design).
            migrationBuilder.Sql(@"
INSERT INTO reports
(
    company_id,
    created_by_user_id,
    report_type,
    warehouse_id,
    time_from,
    time_to,
    status,
    created_at,
    completed_at,
    error_message,
    parameters_json,
    summary_json,
    data_json,
    schema_version,
    pdf_url,
    pdf_file_name,
    pdf_content_hash,
    pdf_generated_at
)
SELECT
    rr.company_id,
    rr.created_by_user_id,
    rr.report_type,
    rr.warehouse_id,
    rr.time_from,
    rr.time_to,
    rr.status,
    rr.created_at,
    rr.completed_at,
    rr.error_message,
    rr.parameters_json,
    rres.summary_json,
    rres.data_json,
    rres.schema_version,
    NULL,
    NULL,
    NULL,
    NULL
FROM report_requests rr
LEFT JOIN report_results rres ON rres.report_request_id = rr.id;
");

            migrationBuilder.DropTable(
                name: "report_artifacts");

            migrationBuilder.DropTable(
                name: "report_results");

            migrationBuilder.DropTable(
                name: "report_requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.CreateTable(
                name: "report_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    parameters_json = table.Column<string>(type: "jsonb", nullable: true),
                    report_type = table.Column<string>(type: "character varying", nullable: true),
                    status = table.Column<string>(type: "character varying", nullable: true),
                    time_from = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    time_to = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("report_requests_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_requests_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_requests_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_report_requests_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report_artifacts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_request_id = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character varying", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    file_name = table.Column<string>(type: "character varying", nullable: true),
                    format = table.Column<string>(type: "character varying", nullable: true),
                    storage_provider = table.Column<string>(type: "character varying", nullable: true),
                    url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("report_artifacts_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_artifacts_report_request_id",
                        column: x => x.report_request_id,
                        principalTable: "report_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_results",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    report_request_id = table.Column<int>(type: "integer", nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    schema_version = table.Column<string>(type: "character varying", nullable: true),
                    summary_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("report_results_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_results_report_request_id",
                        column: x => x.report_request_id,
                        principalTable: "report_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_artifacts_report_request_id",
                table: "report_artifacts",
                column: "report_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_requests_company_id",
                table: "report_requests",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_requests_created_by_user_id",
                table: "report_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_requests_warehouse_id",
                table: "report_requests",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_results_report_request_id",
                table: "report_results",
                column: "report_request_id",
                unique: true);
        }
    }
}
