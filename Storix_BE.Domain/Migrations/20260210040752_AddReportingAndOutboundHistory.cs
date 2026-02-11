using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingAndOutboundHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "outbound_orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_runs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying", nullable: true),
                    model_version = table.Column<string>(type: "character varying", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    parameters_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ai_runs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_runs_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbound_order_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    outbound_order_id = table.Column<int>(type: "integer", nullable: false),
                    old_status = table.Column<string>(type: "character varying", nullable: true),
                    new_status = table.Column<string>(type: "character varying", nullable: true),
                    changed_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("outbound_order_status_history_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbound_order_status_history_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_outbound_order_status_history_outbound_order_id",
                        column: x => x.outbound_order_id,
                        principalTable: "outbound_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_requests",
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
                    parameters_json = table.Column<string>(type: "jsonb", nullable: true)
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
                    format = table.Column<string>(type: "character varying", nullable: true),
                    storage_provider = table.Column<string>(type: "character varying", nullable: true),
                    url = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "character varying", nullable: true),
                    content_hash = table.Column<string>(type: "character varying", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                    summary_json = table.Column<string>(type: "jsonb", nullable: true),
                    data_json = table.Column<string>(type: "jsonb", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    schema_version = table.Column<string>(type: "character varying", nullable: true)
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
                name: "IX_ai_runs_company_id",
                table: "ai_runs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_order_status_history_changed_by_user_id",
                table: "outbound_order_status_history",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_outbound_order_status_history_outbound_order_id",
                table: "outbound_order_status_history",
                column: "outbound_order_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_runs");

            migrationBuilder.DropTable(
                name: "outbound_order_status_history");

            migrationBuilder.DropTable(
                name: "report_artifacts");

            migrationBuilder.DropTable(
                name: "report_results");

            migrationBuilder.DropTable(
                name: "report_requests");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "outbound_orders");
        }
    }
}
