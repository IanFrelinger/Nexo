using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization export result
/// </summary>
public record OptimizationExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int DataRecordsExported { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 