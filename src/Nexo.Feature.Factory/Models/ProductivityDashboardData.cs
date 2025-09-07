using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity dashboard data
/// </summary>
public record ProductivityDashboardData
{
    public DateTime GeneratedAt { get; init; }
    public ProductivitySummary Summary { get; init; } = new();
    public List<DashboardWidget> Widgets { get; init; } = new();
    public List<Alert> Alerts { get; init; } = new();
    public Dictionary<string, object> CustomData { get; init; } = new();
} 