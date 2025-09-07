using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity metrics request
/// </summary>
public record ProductivityMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string? UserId { get; init; }
    public List<string> Metrics { get; init; } = new();
    public string GroupBy { get; init; } = string.Empty;
} 