using System;

namespace Storix_BE.Domain.Models;

public partial class AiRun
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string? Type { get; set; }

    public string? ModelVersion { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string? ParametersJson { get; set; }

    public virtual Company? Company { get; set; }
}

