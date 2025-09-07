using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity validation result
/// </summary>
public record ProductivityValidationResult
{
    public bool IsValidated { get; init; }
    public double AchievedMultiplier { get; init; }
    public double TargetMultiplier { get; init; }
    public double ValidationScore { get; init; }
    public List<ValidationCriterion> Criteria { get; init; } = new();
    public List<string> ValidationIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
} 