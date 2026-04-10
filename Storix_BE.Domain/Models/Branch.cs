using System;
using System.Collections.Generic;

namespace Storix_BE.Domain.Models;

public partial class Branch
{
    public int Id { get; set; }

    public int? CompanyId { get; set; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Company? Company { get; set; }

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
