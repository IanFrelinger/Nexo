using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS pod log entry
/// </summary>
public record AKSPodLogEntry
{
    public string PodName { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
} 