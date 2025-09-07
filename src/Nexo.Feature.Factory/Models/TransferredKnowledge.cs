using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Transferred knowledge
/// </summary>
public record TransferredKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string SourceProjectId { get; init; } = string.Empty;
    public string TargetProjectId { get; init; } = string.Empty;
    public string KnowledgeType { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public DateTime TransferredAt { get; init; }
    public Dictionary<string, object> TransferData { get; init; } = new();
} 