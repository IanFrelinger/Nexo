using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence search request
/// </summary>
public record CollectiveIntelligenceSearchRequest
{
    public string Query { get; init; } = string.Empty;
    public List<string> SearchTypes { get; init; } = new();
    public List<string> Filters { get; init; } = new();
    public int MaxResults { get; init; }
    public bool EnableSemanticSearch { get; init; }
    public Dictionary<string, object> SearchParameters { get; init; } = new();
} 