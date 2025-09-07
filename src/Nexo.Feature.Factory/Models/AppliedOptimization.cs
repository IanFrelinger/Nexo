using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Applied optimization
/// </summary>
public record AppliedOptimization
{
    public string SuggestionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Impact { get; init; }
    public List<string> AppliedChanges { get; init; } = new();
    public DateTime AppliedAt { get; init; }
    public Dictionary<string, object> ApplicationData { get; init; } = new();
} 