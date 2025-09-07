using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization report request
/// </summary>
public record OptimizationReportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> ReportTypes { get; init; } = new();
    public string Format { get; init; } = string.Empty;
    public bool IncludeCharts { get; init; }
    public bool IncludeRecommendations { get; init; }
    public Dictionary<string, object> ReportParameters { get; init; } = new();
} 