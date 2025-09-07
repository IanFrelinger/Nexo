using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Feature knowledge sharing result
/// </summary>
public record FeatureKnowledgeSharingResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<SharedKnowledge> SharedKnowledge { get; init; } = new();
    public int KnowledgeItemsShared { get; init; }
    public int RecipientsNotified { get; init; }
    public double SharingSuccessRate { get; init; }
    public TimeSpan SharingDuration { get; init; }
    public Dictionary<string, double> SharingMetrics { get; init; } = new();
    public DateTime SharedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 