using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity dashboard request
/// </summary>
public record ProductivityDashboardRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> Widgets { get; init; } = new();
    public string RefreshInterval { get; init; } = string.Empty;
} 