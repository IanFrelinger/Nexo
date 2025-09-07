using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Cross-project learning request
/// </summary>
public record CrossProjectLearningRequest
{
    public List<string> SourceProjectIds { get; init; } = new();
    public string TargetProjectId { get; init; } = string.Empty;
    public List<string> LearningTypes { get; init; } = new();
    public bool EnablePatternTransfer { get; init; }
    public bool EnableKnowledgeTransfer { get; init; }
    public Dictionary<string, object> LearningParameters { get; init; } = new();
} 