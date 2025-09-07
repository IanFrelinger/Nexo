using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence synchronization request
/// </summary>
public record CollectiveIntelligenceSyncRequest
{
    public List<string> NodeIds { get; init; } = new();
    public string SyncType { get; init; } = string.Empty;
    public bool EnableBidirectionalSync { get; init; }
    public bool EnableConflictResolution { get; init; }
    public Dictionary<string, object> SyncParameters { get; init; } = new();
} 