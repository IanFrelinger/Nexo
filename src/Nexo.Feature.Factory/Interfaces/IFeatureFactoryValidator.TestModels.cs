using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// End-to-end test request
/// </summary>
public record EndToEndTestRequest
{
    public List<TestScenario> Scenarios { get; init; } = new();
    public bool IncludeDeployment { get; init; }
    public bool IncludeIntegration { get; init; }
    public bool IncludeUserAcceptance { get; init; }
    public TimeSpan TestTimeout { get; init; }
    public Dictionary<string, object> TestParameters { get; init; } = new();
}

/// <summary>
/// End-to-end test result
/// </summary>
public record EndToEndTestResult
{
    public List<IndividualEndToEndTestResult> TestResults { get; init; } = new();
    public int TotalTests { get; init; }
    public int PassedTests { get; init; }
    public int FailedTests { get; init; }
    public double SuccessRate { get; init; }
    public TimeSpan TotalTestTime { get; init; }
    public List<string> CriticalIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Individual end-to-end test result
/// </summary>
public record IndividualEndToEndTestResult
{
    public string TestId { get; init; } = string.Empty;
    public string ScenarioId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public TimeSpan Duration { get; init; }
    public List<TestPhaseResult> PhaseResults { get; init; } = new();
    public List<string> Issues { get; init; } = new();
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// Test phase result
/// </summary>
public record TestPhaseResult
{
    public string PhaseName { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object> PhaseData { get; init; } = new();
}
