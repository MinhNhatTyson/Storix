using System;
using System.Collections.Generic;

namespace Storix_BE.Domain.Models;

public partial class TransferOrder
{
    public int Id { get; set; }

    public int? SourceWarehouseId { get; set; }

    public int? DestinationWarehouseId { get; set; }

    public int? CreatedBy { get; set; }

    public int? OutboundOrderId { get; set; }

    public int? InboundOrderId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual InboundOrder? InboundOrder { get; set; }

    public virtual OutboundOrder? OutboundOrder { get; set; }

    public virtual Warehouse? DestinationWarehouse { get; set; }

    public virtual Warehouse? SourceWarehouse { get; set; }

    public virtual ICollection<TransferOrderItem> TransferOrderItems { get; set; } = new List<TransferOrderItem>();
}
