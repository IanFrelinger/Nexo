using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Feature knowledge sharing request
/// </summary>
public record FeatureKnowledgeSharingRequest
{
    public List<FeatureKnowledge> KnowledgeItems { get; init; } = new();
    public string ProjectId { get; init; } = string.Empty;
    public string OrganizationId { get; init; } = string.Empty;
    public bool EnableAnonymization { get; init; }
    public bool EnableEncryption { get; init; }
    public List<string> SharingScopes { get; init; } = new();
    public Dictionary<string, object> SharingParameters { get; init; } = new();
} 