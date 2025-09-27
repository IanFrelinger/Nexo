using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Validation report request
/// </summary>
public record ValidationReportRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<string> ReportTypes { get; init; } = new();
    public string Format { get; init; } = string.Empty;
    public bool IncludeCharts { get; init; }
    public bool IncludeRecommendations { get; init; }
}

/// <summary>
/// Validation report result
/// </summary>
public record ValidationReportResult
{
    public bool IsSuccessful { get; init; }
    public string ReportPath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public ValidationSummary Summary { get; init; } = new();
    public List<ValidationSection> Sections { get; init; } = new();
    public List<string> KeyFindings { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Validation summary
/// </summary>
public record ValidationSummary
{
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int FailedTests { get; init; }
    public double OverallSuccessRate { get; init; }
    public double AveragePerformanceScore { get; init; }
    public double QualityScore { get; init; }
    public string OverallStatus { get; init; } = string.Empty;
}

/// <summary>
/// Validation section
/// </summary>
public record ValidationSection
{
    public string SectionName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int TestCount { get; init; }
    public int PassedCount { get; init; }
    public int FailedCount { get; init; }
    public double SuccessRate { get; init; }
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Production readiness request
/// </summary>
public record ProductionReadinessRequest
{
    public List<TestScenario> Scenarios { get; init; } = new();
    public TimeSpan MaxGenerationTime { get; init; } = TimeSpan.FromDays(2);
    public double MinimumQualityScore { get; init; }
    public double MinimumPerformanceScore { get; init; }
    public List<string> ProductionCriteria { get; init; } = new();
}

/// <summary>
/// Production readiness result
/// </summary>
public record ProductionReadinessResult
{
    public bool IsProductionReady { get; init; }
    public double ReadinessScore { get; init; }
    public TimeSpan AverageGenerationTime { get; init; }
    public double QualityScore { get; init; }
    public double PerformanceScore { get; init; }
    public List<ProductionCriterion> Criteria { get; init; } = new();
    public List<string> BlockingIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Production criterion
/// </summary>
public record ProductionCriterion
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsMet { get; init; }
    public double Score { get; init; }
    public string? Issue { get; init; }
    public string Impact { get; init; } = string.Empty;
}
