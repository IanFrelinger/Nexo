using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Transferred pattern
/// </summary>
public record TransferredPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string SourceProjectId { get; init; } = string.Empty;
    public string TargetProjectId { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public double AdaptationScore { get; init; }
    public DateTime TransferredAt { get; init; }
    public Dictionary<string, object> AdaptationData { get; init; } = new();
} 