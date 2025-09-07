using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence synchronization result
/// </summary>
public record CollectiveIntelligenceSyncResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<SyncResult> SyncResults { get; init; } = new();
    public int NodesSynchronized { get; init; }
    public int ConflictsResolved { get; init; }
    public double SyncSuccessRate { get; init; }
    public TimeSpan SyncDuration { get; init; }
    public Dictionary<string, double> SyncMetrics { get; init; } = new();
    public DateTime SyncedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 