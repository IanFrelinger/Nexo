using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Development comparison request
/// </summary>
public record DevelopmentComparisonRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> ComparisonMetrics { get; init; } = new();
} 