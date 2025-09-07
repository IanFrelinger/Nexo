using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity export request
/// </summary>
public record ProductivityExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> Metrics { get; init; } = new();
    public string? FilePath { get; init; }
    public bool IncludeCharts { get; init; }
} 