using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence search result
/// </summary>
public record CollectiveIntelligenceSearchResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<SearchResult> SearchResults { get; init; } = new();
    public int TotalResults { get; init; }
    public int ResultsReturned { get; init; }
    public TimeSpan SearchDuration { get; init; }
    public double SearchRelevance { get; init; }
    public Dictionary<string, double> SearchMetrics { get; init; } = new();
    public DateTime SearchedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 