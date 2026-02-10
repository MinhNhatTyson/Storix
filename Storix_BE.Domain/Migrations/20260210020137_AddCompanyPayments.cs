using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "types",
                newName: "product_types");

            migrationBuilder.AddColumn<string>(
                name: "avatar",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "total_price",
                table: "outbound_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "price",
                table: "outbound_order_items",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "inbound_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "expected_date",
                table: "inbound_requests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "final_price",
                table: "inbound_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "inbound_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "order_discount",
                table: "inbound_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "total_price",
                table: "inbound_requests",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "staff_id",
                table: "inbound_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "discount",
                table: "inbound_order_items",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "price",
                table: "inbound_order_items",
                type: "double precision",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "product_types",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('types_id_seq'::regclass)",
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "company_id",
                table: "product_types",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "company_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    payment_status = table.Column<string>(type: "character varying", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_method = table.Column<string>(type: "character varying", nullable: false),
                    paid_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("company_payments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_company_payments_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_prices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    price = table.Column<double>(type: "double precision", nullable: true),
                    line_discount = table.Column<double>(type: "double precision", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_prices.product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    token = table.Column<string>(type: "text", nullable: true),
                    expired_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("refresh_tokens.id", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens.id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbound_orders_staff_id",
                table: "outbound_orders",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_inbound_orders_staff_id",
                table: "inbound_orders",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_company_id",
                table: "product_types",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_company_payments_company_id",
                table: "company_payments",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_prices_product_id",
                table: "product_prices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inbound_orders_staff_id",
                table: "inbound_orders",
                column: "staff_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_outbound_orders_staff_id",
                table: "outbound_orders",
                column: "staff_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_product_types_company_id",
                table: "product_types",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inbound_orders_staff_id",
                table: "inbound_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_outbound_orders_staff_id",
                table: "outbound_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_product_types_company_id",
                table: "product_types");

            migrationBuilder.DropTable(
                name: "company_payments");

            migrationBuilder.DropTable(
                name: "product_prices");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_outbound_orders_staff_id",
                table: "outbound_orders");

            migrationBuilder.DropIndex(
                name: "IX_inbound_orders_staff_id",
                table: "inbound_orders");

            migrationBuilder.DropIndex(
                name: "IX_product_types_company_id",
                table: "product_types");

            migrationBuilder.DropColumn(
                name: "avatar",
                table: "users");

            migrationBuilder.DropColumn(
                name: "image",
                table: "products");

            migrationBuilder.DropColumn(
                name: "total_price",
                table: "outbound_requests");

            migrationBuilder.DropColumn(
                name: "price",
                table: "outbound_order_items");

            migrationBuilder.DropColumn(
                name: "code",
                table: "inbound_requests");

            migrationBuilder.DropColumn(
                name: "expected_date",
                table: "inbound_requests");

            migrationBuilder.DropColumn(
                name: "final_price",
                table: "inbound_requests");

            migrationBuilder.DropColumn(
                name: "note",
                table: "inbound_requests");

            migrationBuilder.DropColumn(
                name: "order_discount",
                table: "inbound_requests");

            migrationBuilder.DropColumn(
                name: "total_price",
                table: "inbound_requests");

            migrationBuilder.DropColumn(
                name: "staff_id",
                table: "inbound_orders");

            migrationBuilder.DropColumn(
                name: "discount",
                table: "inbound_order_items");

            migrationBuilder.DropColumn(
                name: "price",
                table: "inbound_order_items");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "product_types");

            migrationBuilder.RenameTable(
                name: "product_types",
                newName: "types");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "types",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValueSql: "nextval('types_id_seq'::regclass)")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
