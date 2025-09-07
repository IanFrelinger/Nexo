using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization report result
/// </summary>
public record OptimizationReportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ReportPath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public OptimizationSummary Summary { get; init; } = new();
    public List<OptimizationSection> Sections { get; init; } = new();
    public List<string> KeyFindings { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 