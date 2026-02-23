using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "company_payments",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "momo_trans_id",
                table: "company_payments",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "plan_type",
                table: "company_payments",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "subscription_id",
                table: "company_payments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    plan_type = table.Column<string>(type: "character varying", nullable: false),
                    status = table.Column<string>(type: "character varying", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("subscriptions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_subscriptions_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_company_payments_idempotency_key",
                table: "company_payments",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_company_payments_subscription_id",
                table: "company_payments",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_company_id_status",
                table: "subscriptions",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_subscriptions_end_date",
                table: "subscriptions",
                column: "end_date");

            migrationBuilder.AddForeignKey(
                name: "fk_company_payments_subscription_id",
                table: "company_payments",
                column: "subscription_id",
                principalTable: "subscriptions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_company_payments_subscription_id",
                table: "company_payments");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_company_payments_idempotency_key",
                table: "company_payments");

            migrationBuilder.DropIndex(
                name: "IX_company_payments_subscription_id",
                table: "company_payments");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "company_payments");

            migrationBuilder.DropColumn(
                name: "momo_trans_id",
                table: "company_payments");

            migrationBuilder.DropColumn(
                name: "plan_type",
                table: "company_payments");

            migrationBuilder.DropColumn(
                name: "subscription_id",
                table: "company_payments");
        }
    }
}
