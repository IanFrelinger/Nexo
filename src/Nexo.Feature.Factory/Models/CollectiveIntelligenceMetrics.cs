using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Collective intelligence metrics
/// </summary>
public record CollectiveIntelligenceMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalKnowledgeItems { get; init; }
    public int ActiveProjects { get; init; }
    public int ConnectedOrganizations { get; init; }
    public double KnowledgeSharingRate { get; init; }
    public double CrossProjectLearningRate { get; init; }
    public List<IntelligenceTrend> IntelligenceTrends { get; init; } = new();
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
} 