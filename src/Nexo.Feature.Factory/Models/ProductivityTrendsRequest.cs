using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity trends request
/// </summary>
public record ProductivityTrendsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string Granularity { get; init; } = string.Empty;
    public List<string> Metrics { get; init; } = new();
} 