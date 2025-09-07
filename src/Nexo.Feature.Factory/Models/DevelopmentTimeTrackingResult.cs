using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Development time tracking result
/// </summary>
public record DevelopmentTimeTrackingResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public TimeSpan TotalDevelopmentTime { get; init; }
    public List<DevelopmentPhase> Phases { get; init; } = new();
    public DateTime TrackedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 