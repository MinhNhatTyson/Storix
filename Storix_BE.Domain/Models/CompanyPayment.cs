using System;

namespace Storix_BE.Domain.Models;

public partial class CompanyPayment
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;
}
