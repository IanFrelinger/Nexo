using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Cross-project learning result
/// </summary>
public record CrossProjectLearningResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<TransferredKnowledge> TransferredKnowledge { get; init; } = new();
    public List<TransferredPattern> TransferredPatterns { get; init; } = new();
    public int KnowledgeItemsTransferred { get; init; }
    public int PatternsTransferred { get; init; }
    public double TransferSuccessRate { get; init; }
    public TimeSpan TransferDuration { get; init; }
    public Dictionary<string, double> TransferMetrics { get; init; } = new();
    public DateTime TransferredAt { get; init; }
    public string? ErrorMessage { get; init; }
} 