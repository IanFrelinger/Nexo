using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Learning data export models and data structures
/// </summary>
public partial interface IAILearningSystem
{
    // This partial interface contains export models
}

/// <summary>
/// Learning data export request
/// </summary>
public record LearningDataExportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Format { get; init; } = string.Empty;
    public List<string> DataTypes { get; init; } = new();
    public bool IncludeMetadata { get; init; }
    public string? FilePath { get; init; }
}

/// <summary>
/// Learning data export result
/// </summary>
public record LearningDataExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int DataRecordsExported { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
