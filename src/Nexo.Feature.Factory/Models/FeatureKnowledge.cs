using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Feature knowledge
/// </summary>
public record FeatureKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string FeatureId { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string KnowledgeType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public List<string> Categories { get; init; } = new();
    public double QualityScore { get; init; }
    public int UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdated { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
} 