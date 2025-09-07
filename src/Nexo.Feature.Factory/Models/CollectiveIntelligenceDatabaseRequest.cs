using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence database request
/// </summary>
public record CollectiveIntelligenceDatabaseRequest
{
    public string DatabaseName { get; init; } = string.Empty;
    public List<string> DatabaseTypes { get; init; } = new();
    public bool EnableDistributedStorage { get; init; }
    public bool EnableRealTimeIndexing { get; init; }
    public Dictionary<string, object> DatabaseParameters { get; init; } = new();
} 