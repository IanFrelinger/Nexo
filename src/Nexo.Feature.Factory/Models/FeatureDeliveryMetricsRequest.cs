using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Feature delivery metrics request
/// </summary>
public record FeatureDeliveryMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public string? TeamId { get; init; }
    public List<string> Metrics { get; init; } = new();
} 