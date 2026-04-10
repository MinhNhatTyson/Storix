using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesAndInventoryThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_types_company_id",
                table: "product_types");

            migrationBuilder.DropForeignKey(
                name: "FK_products_product_types_ProductTypeId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_storage_zones_product_types_ProductTypeId",
                table: "storage_zones");

            migrationBuilder.DropIndex(
                name: "IX_storage_zones_ProductTypeId",
                table: "storage_zones");

            migrationBuilder.DropIndex(
                name: "IX_products_ProductTypeId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_types_company_id",
                table: "product_types");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "storage_zones");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "product_types");

            migrationBuilder.RenameColumn(
                name: "IsVulnerable",
                table: "storage_zones",
                newName: "isVulnerable");

            migrationBuilder.AddColumn<int>(
                name: "branch_id",
                table: "warehouses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "stock_counts_tickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approved_by",
                table: "stock_counts_tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "planned_at",
                table: "stock_counts_tickets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "scope_id",
                table: "stock_counts_tickets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope_type",
                table: "stock_counts_tickets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "counted_at",
                table: "stock_count_items",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "counted_by",
                table: "stock_count_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "final_quantity",
                table: "stock_count_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "location_id",
                table: "stock_count_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "recounted_at",
                table: "stock_count_items",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recounted_by",
                table: "stock_count_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recounted_quantity",
                table: "stock_count_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "recommendations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_counted_at",
                table: "inventory",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_stock",
                table: "inventory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_stock",
                table: "inventory",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying", nullable: true),
                    address = table.Column<string>(type: "character varying", nullable: true),
                    status = table.Column<string>(type: "character varying", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("branches_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_branches_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    is_global = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    notification_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    read_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_notifications_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_notifications_notification_id_fkey",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_notifications_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_branch_id",
                table: "warehouses",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_tickets_scope_id",
                table: "stock_counts_tickets",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_items_location_id",
                table: "stock_count_items",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_company_id",
                table: "branches",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_notification_id",
                table: "user_notifications",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_user_id",
                table: "user_notifications",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_count_items_location_id",
                table: "stock_count_items",
                column: "location_id",
                principalTable: "inventory_locations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_stock_counts_tickets_scope_id",
                table: "stock_counts_tickets",
                column: "scope_id",
                principalTable: "storage_zones",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_warehouses_branch_id",
                table: "warehouses",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_stock_count_items_location_id",
                table: "stock_count_items");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_counts_tickets_scope_id",
                table: "stock_counts_tickets");

            migrationBuilder.DropForeignKey(
                name: "fk_warehouses_branch_id",
                table: "warehouses");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_branch_id",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_stock_counts_tickets_scope_id",
                table: "stock_counts_tickets");

            migrationBuilder.DropIndex(
                name: "IX_stock_count_items_location_id",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "warehouses");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "stock_counts_tickets");

            migrationBuilder.DropColumn(
                name: "approved_by",
                table: "stock_counts_tickets");

            migrationBuilder.DropColumn(
                name: "planned_at",
                table: "stock_counts_tickets");

            migrationBuilder.DropColumn(
                name: "scope_id",
                table: "stock_counts_tickets");

            migrationBuilder.DropColumn(
                name: "scope_type",
                table: "stock_counts_tickets");

            migrationBuilder.DropColumn(
                name: "counted_at",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "counted_by",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "final_quantity",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "location_id",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "recounted_at",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "recounted_by",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "recounted_quantity",
                table: "stock_count_items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "recommendations");

            migrationBuilder.DropColumn(
                name: "last_counted_at",
                table: "inventory");

            migrationBuilder.DropColumn(
                name: "max_stock",
                table: "inventory");

            migrationBuilder.DropColumn(
                name: "min_stock",
                table: "inventory");

            migrationBuilder.RenameColumn(
                name: "isVulnerable",
                table: "storage_zones",
                newName: "IsVulnerable");

            migrationBuilder.AddColumn<int>(
                name: "ProductTypeId",
                table: "storage_zones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductTypeId",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "company_id",
                table: "product_types",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_storage_zones_ProductTypeId",
                table: "storage_zones",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_products_ProductTypeId",
                table: "products",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_product_types_company_id",
                table: "product_types",
                column: "company_id");

            migrationBuilder.AddForeignKey(
                name: "fk_product_types_company_id",
                table: "product_types",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_products_product_types_ProductTypeId",
                table: "products",
                column: "ProductTypeId",
                principalTable: "product_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_storage_zones_product_types_ProductTypeId",
                table: "storage_zones",
                column: "ProductTypeId",
                principalTable: "product_types",
                principalColumn: "id");
        }
    }
}
