using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization validation request
/// </summary>
public record OptimizationValidationRequest
{
    public List<string> SuggestionIds { get; init; } = new();
    public string ValidationType { get; init; } = string.Empty;
    public bool EnableSimulation { get; init; }
    public bool EnableTesting { get; init; }
    public Dictionary<string, object> ValidationParameters { get; init; } = new();
} 