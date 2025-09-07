using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence database result
/// </summary>
public record CollectiveIntelligenceDatabaseResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DatabaseId { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
    public List<string> DatabaseTypes { get; init; } = new();
    public long StorageCapacity { get; init; }
    public long UsedStorage { get; init; }
    public int KnowledgeItems { get; init; }
    public int ConnectedNodes { get; init; }
    public DateTime CreatedAt { get; init; }
    public Dictionary<string, object> DatabaseInfo { get; init; } = new();
    public string? ErrorMessage { get; init; }
} 