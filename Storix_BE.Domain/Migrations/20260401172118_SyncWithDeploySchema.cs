using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithDeploySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_type_id",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_zones_type_id",
                table: "storage_zones");

            migrationBuilder.DropColumn(
                name: "zoneType",
                table: "storage_zones");

            migrationBuilder.DropColumn(
                name: "storage_recommendation",
                table: "storage_recommendations");

            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "storage_zones",
                newName: "ProductTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_storage_zones_type_id",
                table: "storage_zones",
                newName: "IX_storage_zones_ProductTypeId");

            migrationBuilder.RenameColumn(
                name: "type_id",
                table: "products",
                newName: "ProductTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_products_type_id",
                table: "products",
                newName: "IX_products_ProductTypeId");

            migrationBuilder.AddColumn<bool>(
                name: "IsVulnerable",
                table: "storage_zones",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isCold",
                table: "storage_zones",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isHighValue",
                table: "storage_zones",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "recommendation_id",
                table: "storage_recommendations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isCold",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isHighValue",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isVulnerable",
                table: "products",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reason",
                table: "outbound_requests",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_code",
                table: "outbound_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expected_quantity",
                table: "outbound_order_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "received_quantity",
                table: "outbound_order_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recommendations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('storage_recommendations_id_seq'::regclass)"),
                    bin_id = table.Column<int>(type: "integer", nullable: true),
                    path = table.Column<string>(type: "text", nullable: true),
                    distance_info = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recommendations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_recommendations_bin_id",
                        column: x => x.bin_id,
                        principalTable: "shelf_level_bins",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_storage_recommendations_recommendation_id",
                table: "storage_recommendations",
                column: "recommendation_id");

            migrationBuilder.CreateIndex(
                name: "IX_recommendations_bin_id",
                table: "recommendations",
                column: "bin_id");

            migrationBuilder.AddForeignKey(
                name: "FK_products_product_types_ProductTypeId",
                table: "products",
                column: "ProductTypeId",
                principalTable: "product_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_storage_recommendations_recommendation_id",
                table: "storage_recommendations",
                column: "recommendation_id",
                principalTable: "recommendations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_storage_zones_product_types_ProductTypeId",
                table: "storage_zones",
                column: "ProductTypeId",
                principalTable: "product_types",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_product_types_ProductTypeId",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_storage_recommendations_recommendation_id",
                table: "storage_recommendations");

            migrationBuilder.DropForeignKey(
                name: "FK_storage_zones_product_types_ProductTypeId",
                table: "storage_zones");

            migrationBuilder.DropTable(
                name: "recommendations");

            migrationBuilder.DropIndex(
                name: "IX_storage_recommendations_recommendation_id",
                table: "storage_recommendations");

            migrationBuilder.DropColumn(
                name: "IsVulnerable",
                table: "storage_zones");

            migrationBuilder.DropColumn(
                name: "isCold",
                table: "storage_zones");

            migrationBuilder.DropColumn(
                name: "isHighValue",
                table: "storage_zones");

            migrationBuilder.DropColumn(
                name: "recommendation_id",
                table: "storage_recommendations");

            migrationBuilder.DropColumn(
                name: "isCold",
                table: "products");

            migrationBuilder.DropColumn(
                name: "isHighValue",
                table: "products");

            migrationBuilder.DropColumn(
                name: "isVulnerable",
                table: "products");

            migrationBuilder.DropColumn(
                name: "reason",
                table: "outbound_requests");

            migrationBuilder.DropColumn(
                name: "reference_code",
                table: "outbound_requests");

            migrationBuilder.DropColumn(
                name: "expected_quantity",
                table: "outbound_order_items");

            migrationBuilder.DropColumn(
                name: "received_quantity",
                table: "outbound_order_items");

            migrationBuilder.RenameColumn(
                name: "ProductTypeId",
                table: "storage_zones",
                newName: "type_id");

            migrationBuilder.RenameIndex(
                name: "IX_storage_zones_ProductTypeId",
                table: "storage_zones",
                newName: "IX_storage_zones_type_id");

            migrationBuilder.RenameColumn(
                name: "ProductTypeId",
                table: "products",
                newName: "type_id");

            migrationBuilder.RenameIndex(
                name: "IX_products_ProductTypeId",
                table: "products",
                newName: "IX_products_type_id");

            migrationBuilder.AddColumn<string>(
                name: "zoneType",
                table: "storage_zones",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_recommendation",
                table: "storage_recommendations",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_products_type_id",
                table: "products",
                column: "type_id",
                principalTable: "product_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_storage_zones_type_id",
                table: "storage_zones",
                column: "type_id",
                principalTable: "product_types",
                principalColumn: "id");
        }
    }
}
