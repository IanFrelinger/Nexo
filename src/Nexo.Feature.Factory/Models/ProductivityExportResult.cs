using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity export result
/// </summary>
public record ProductivityExportResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime ExportedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 