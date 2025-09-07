using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization export request
/// </summary>
public record OptimizationExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> DataTypes { get; init; } = new();
    public bool IncludeMetadata { get; init; }
    public string? FilePath { get; init; }
} 