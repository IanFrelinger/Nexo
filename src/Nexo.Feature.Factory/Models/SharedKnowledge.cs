using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Shared knowledge
/// </summary>
public record SharedKnowledge
{
    public string KnowledgeId { get; init; } = string.Empty;
    public string SharingId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> Recipients { get; init; } = new();
    public DateTime SharedAt { get; init; }
    public Dictionary<string, object> SharingData { get; init; } = new();
} 