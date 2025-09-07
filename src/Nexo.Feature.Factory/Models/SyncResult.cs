using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Sync result
/// </summary>
public record SyncResult
{
    public string NodeId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ItemsSynced { get; init; }
    public int ConflictsResolved { get; init; }
    public DateTime SyncedAt { get; init; }
    public Dictionary<string, object> SyncData { get; init; } = new();
} 