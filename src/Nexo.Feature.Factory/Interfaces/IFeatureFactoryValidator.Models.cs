using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Test scenario request
/// </summary>
public record TestScenarioRequest
{
    public List<string> TestTypes { get; init; } = new();
    public List<string> Domains { get; init; } = new();
    public List<string> Industries { get; init; } = new();
    public List<string> ComplexityLevels { get; init; } = new();
    public int ScenarioCount { get; init; }
    public Dictionary<string, object> CustomParameters { get; init; } = new();
}

/// <summary>
/// Test scenario result
/// </summary>
public record TestScenarioResult
{
    public List<TestScenario> Scenarios { get; init; } = new();
    public int TotalScenarios { get; init; }
    public Dictionary<string, int> ScenariosByType { get; init; } = new();
    public Dictionary<string, int> ScenariosByDomain { get; init; } = new();
    public Dictionary<string, int> ScenariosByComplexity { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Test scenario
/// </summary>
public record TestScenario
{
    public string ScenarioId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string Complexity { get; init; } = string.Empty;
    public string NaturalLanguageDescription { get; init; } = string.Empty;
    public List<string> ExpectedOutcomes { get; init; } = new();
    public List<string> SuccessCriteria { get; init; } = new();
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Feature generation test request
/// </summary>
public record FeatureGenerationTestRequest
{
    public List<TestScenario> Scenarios { get; init; } = new();
    public bool RunInParallel { get; init; }
    public int MaxConcurrentTests { get; init; }
    public TimeSpan TestTimeout { get; init; }
    public bool ValidateOutputs { get; init; }
    public Dictionary<string, object> TestParameters { get; init; } = new();
}

/// <summary>
/// Feature generation test result
/// </summary>
public record FeatureGenerationTestResult
{
    public List<FeatureTestResult> TestResults { get; init; } = new();
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int FailedTests { get; init; }
    public double SuccessRate { get; init; }
    public TimeSpan TotalTestTime { get; init; }
    public TimeSpan AverageTestTime { get; init; }
    public List<string> CommonIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Feature test result
/// </summary>
public record FeatureTestResult
{
    public string TestId { get; init; } = string.Empty;
    public string ScenarioId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public FeatureGenerationResult GeneratedFeature { get; init; } = new();
    public List<string> ValidationErrors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public Dictionary<string, object> TestMetrics { get; init; } = new();
}

/// <summary>
/// Feature generation result
/// </summary>
public record FeatureGenerationResult
{
    public string FeatureId { get; init; } = string.Empty;
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureDescription { get; init; } = string.Empty;
    public string GeneratedCode { get; init; } = string.Empty;
    public List<string> Dependencies { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
