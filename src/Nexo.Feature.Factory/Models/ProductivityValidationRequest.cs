using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity validation request
/// </summary>
public record ProductivityValidationRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double TargetMultiplier { get; init; } = 32.0;
    public string? ProjectId { get; init; }
    public List<string> ValidationCriteria { get; init; } = new();
} 