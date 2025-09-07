using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Dashboard widget
/// </summary>
public record DashboardWidget
{
    public string WidgetId { get; init; } = string.Empty;
    public string WidgetType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public Dictionary<string, object> Data { get; init; } = new();
    public DateTime LastUpdated { get; init; }
} 