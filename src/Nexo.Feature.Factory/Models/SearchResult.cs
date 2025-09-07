using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Search result
/// </summary>
public record SearchResult
{
    public string ResultId { get; init; } = string.Empty;
    public string ResultType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Dictionary<string, object> ResultData { get; init; } = new();
} 