using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Development phase tracking
/// </summary>
public record DevelopmentPhase
{
    public string PhaseName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public TimeSpan Duration { get; init; }
    public string Status { get; init; } = string.Empty;
    public Dictionary<string, object> PhaseData { get; init; } = new();
} 