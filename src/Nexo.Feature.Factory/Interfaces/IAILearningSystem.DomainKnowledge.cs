using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Domain knowledge accumulation models and data structures
/// </summary>
public partial interface IAILearningSystem
{
    // This partial interface contains domain knowledge models
}

/// <summary>
/// Domain knowledge accumulation request
/// </summary>
public record DomainKnowledgeAccumulationRequest
{
    public List<DomainKnowledge> KnowledgeItems { get; init; } = new();
    public string Domain { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public bool EnableKnowledgeGraph { get; init; }
    public bool EnableSemanticSearch { get; init; }
    public Dictionary<string, object> AccumulationParameters { get; init; } = new();
}

/// <summary>
/// Domain knowledge
/// </summary>
public record DomainKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public List<string> RelatedConcepts { get; init; } = new();
    public double Relevance { get; init; }
    public DateTime AcquiredAt { get; init; }
    public string Source { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Domain knowledge accumulation result
/// </summary>
public record DomainKnowledgeAccumulationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<AccumulatedKnowledge> AccumulatedKnowledge { get; init; } = new();
    public int KnowledgeItemsProcessed { get; init; }
    public int NewKnowledgeItems { get; init; }
    public int UpdatedKnowledgeItems { get; init; }
    public double KnowledgeCoverage { get; init; }
    public TimeSpan AccumulationDuration { get; init; }
    public Dictionary<string, double> CoverageMetrics { get; init; } = new();
    public DateTime AccumulatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Accumulated knowledge
/// </summary>
public record AccumulatedKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public int ReferenceCount { get; init; }
    public DateTime LastUpdated { get; init; }
    public List<string> RelatedKnowledge { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
