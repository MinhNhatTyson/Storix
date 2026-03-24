using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    public partial class AddExpectedAndReceivedQuantityToOutboundOrderItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE outbound_order_items
ADD COLUMN IF NOT EXISTS expected_quantity integer NULL;");

            migrationBuilder.Sql(@"
ALTER TABLE outbound_order_items
ADD COLUMN IF NOT EXISTS received_quantity integer NULL;");

            migrationBuilder.Sql(@"
UPDATE outbound_order_items
SET expected_quantity = COALESCE(expected_quantity, quantity),
    received_quantity = COALESCE(received_quantity, quantity)
WHERE expected_quantity IS NULL
   OR received_quantity IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE outbound_order_items
DROP COLUMN IF EXISTS expected_quantity;");

            migrationBuilder.Sql(@"
ALTER TABLE outbound_order_items
DROP COLUMN IF EXISTS received_quantity;");
        }
    }
}
