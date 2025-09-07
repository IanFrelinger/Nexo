using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization application request
/// </summary>
public record OptimizationApplicationRequest
{
    public List<string> SuggestionIds { get; init; } = new();
    public string ApplicationType { get; init; } = string.Empty;
    public bool EnableValidation { get; init; }
    public bool EnableRollback { get; init; }
    public Dictionary<string, object> ApplicationParameters { get; init; } = new();
} 