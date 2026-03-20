using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Storix_BE.Domain.Migrations
{
    public partial class EnforceOutboundOrderQuantityAndPricingMapping : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'outbound_order_items'
          AND column_name = 'pricing_method'
          AND udt_name = 'float8'
    ) THEN
        ALTER TABLE outbound_order_items
        ALTER COLUMN pricing_method TYPE character varying
        USING pricing_method::text;
    END IF;
END $$;");

            migrationBuilder.Sql(@"
ALTER TABLE outbound_order_items
ADD COLUMN IF NOT EXISTS pricing_method character varying NULL;");

            migrationBuilder.Sql(@"
ALTER TABLE outbound_order_items
ADD COLUMN IF NOT EXISTS cost_price double precision NULL;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION enforce_outbound_order_item_qty_match_request()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_order_id int;
    v_request_id int;
    v_mismatch_count int;
BEGIN
    v_order_id := COALESCE(NEW.outbound_order_id, OLD.outbound_order_id);

    IF v_order_id IS NULL THEN
        RETURN COALESCE(NEW, OLD);
    END IF;

    SELECT oi.outbound_request_id
      INTO v_request_id
      FROM outbound_order_items oi
     WHERE oi.outbound_order_id = v_order_id
       AND oi.outbound_request_id IS NOT NULL
     LIMIT 1;

    IF v_request_id IS NULL THEN
        RAISE EXCEPTION 'OutboundOrder % has no linked OutboundRequest items for verification.', v_order_id;
    END IF;

    WITH req AS (
        SELECT product_id, SUM(COALESCE(quantity, 0)) AS qty
          FROM outbound_order_items
         WHERE outbound_request_id = v_request_id
           AND outbound_order_id IS NULL
         GROUP BY product_id
    ), ord AS (
        SELECT product_id, SUM(COALESCE(quantity, 0)) AS qty
          FROM outbound_order_items
         WHERE outbound_order_id = v_order_id
         GROUP BY product_id
    ), cmp AS (
        SELECT COALESCE(req.product_id, ord.product_id) AS product_id,
               COALESCE(req.qty, 0) AS req_qty,
               COALESCE(ord.qty, 0) AS ord_qty
          FROM req
          FULL OUTER JOIN ord ON req.product_id = ord.product_id
    )
    SELECT COUNT(*)
      INTO v_mismatch_count
      FROM cmp
     WHERE req_qty <> ord_qty;

    IF v_mismatch_count > 0 THEN
        RAISE EXCEPTION 'OutboundOrder % item quantities must match requested quantities in OutboundRequest %.', v_order_id, v_request_id;
    END IF;

    RETURN COALESCE(NEW, OLD);
END;
$$;");

            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_enforce_outbound_order_item_qty_match_request ON outbound_order_items;

CREATE TRIGGER trg_enforce_outbound_order_item_qty_match_request
AFTER INSERT OR UPDATE OR DELETE ON outbound_order_items
FOR EACH ROW
EXECUTE FUNCTION enforce_outbound_order_item_qty_match_request();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_enforce_outbound_order_item_qty_match_request ON outbound_order_items;");

            migrationBuilder.Sql(@"
DROP FUNCTION IF EXISTS enforce_outbound_order_item_qty_match_request();");
        }
    }
}
