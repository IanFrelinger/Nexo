using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Represents a usage pattern discovered in the system.
/// </summary>
public record UsagePattern
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public int Frequency { get; init; }
    public TimeSpan Duration { get; init; }
    public List<string> Triggers { get; init; } = new();
    public List<string> Actions { get; init; } = new();
    public DateTime FirstDetected { get; init; }
    public DateTime LastDetected { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
