using System;
using System.Collections.Generic;
using Nexo.Feature.Factory.Enums;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Development time tracking request
/// </summary>
public record DevelopmentTimeTrackingRequest
{
    public string FeatureId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public DevelopmentApproach Approach { get; init; }
    public List<DevelopmentPhase> Phases { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
} 