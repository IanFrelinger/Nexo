using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Industry pattern
/// </summary>
public record IndustryPattern
{
    public string PatternId { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string PatternType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Prevalence { get; init; }
    public double Effectiveness { get; init; }
    public List<string> Companies { get; init; } = new();
    public List<string> UseCases { get; init; } = new();
    public DateTime FirstObserved { get; init; }
    public DateTime LastObserved { get; init; }
    public Dictionary<string, object> PatternData { get; init; } = new();
} 