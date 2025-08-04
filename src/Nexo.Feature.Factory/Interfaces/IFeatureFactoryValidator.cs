using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Service for validating Feature Factory capabilities and performance
/// </summary>
public interface IFeatureFactoryValidator
{
    /// <summary>
    /// Creates comprehensive test scenarios for Feature Factory validation
    /// </summary>
    /// <param name="scenarioRequest">Test scenario request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test scenarios</returns>
    Task<TestScenarioResult> CreateTestScenariosAsync(TestScenarioRequest scenarioRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Implements real-world feature generation tests
    /// </summary>
    /// <param name="testRequest">Feature generation test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results</returns>
    Task<FeatureGenerationTestResult> RunFeatureGenerationTestsAsync(FeatureGenerationTestRequest testRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds performance benchmarking capabilities
    /// </summary>
    /// <param name="benchmarkRequest">Performance benchmark request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Benchmark results</returns>
    Task<PerformanceBenchmarkResult> RunPerformanceBenchmarksAsync(PerformanceBenchmarkRequest benchmarkRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates validation reporting system
    /// </summary>
    /// <param name="reportRequest">Validation report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation report</returns>
    Task<ValidationReportResult> GenerateValidationReportAsync(ValidationReportRequest reportRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that Feature Factory generates production-ready features in 2 days
    /// </summary>
    /// <param name="validationRequest">Production readiness validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Production readiness validation result</returns>
    Task<ProductionReadinessResult> ValidateProductionReadinessAsync(ProductionReadinessRequest validationRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs end-to-end validation tests
    /// </summary>
    /// <param name="e2eRequest">End-to-end test request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>End-to-end test results</returns>
    Task<EndToEndTestResult> RunEndToEndTestsAsync(EndToEndTestRequest e2eRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates code quality and standards
    /// </summary>
    /// <param name="qualityRequest">Code quality validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Code quality validation result</returns>
    Task<CodeQualityResult> ValidateCodeQualityAsync(CodeQualityRequest qualityRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates security compliance
    /// </summary>
    /// <param name="securityRequest">Security validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security validation result</returns>
    Task<SecurityValidationResult> ValidateSecurityComplianceAsync(SecurityValidationRequest securityRequest, CancellationToken cancellationToken = default);
}

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
/// Performance benchmark request
/// </summary>
public record PerformanceBenchmarkRequest
{
    public List<string> BenchmarkTypes { get; init; } = new();
    public int IterationCount { get; init; }
    public TimeSpan BenchmarkDuration { get; init; }
    public bool IncludeLoadTesting { get; init; }
    public bool IncludeStressTesting { get; init; }
    public Dictionary<string, object> BenchmarkParameters { get; init; } = new();
}

/// <summary>
/// Performance benchmark result
/// </summary>
public record PerformanceBenchmarkResult
{
    public List<BenchmarkResult> Results { get; init; } = new();
    public PerformanceMetrics OverallMetrics { get; init; } = new();
    public LoadTestResult LoadTest { get; init; } = new();
    public StressTestResult StressTest { get; init; } = new();
    public List<PerformanceRecommendation> Recommendations { get; init; } = new();
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Benchmark result
/// </summary>
public record BenchmarkResult
{
    public string BenchmarkType { get; init; } = string.Empty;
    public int IterationCount { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public TimeSpan MinDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }
    public double Throughput { get; init; }
    public double SuccessRate { get; init; }
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public record PerformanceMetrics
{
    public TimeSpan AverageResponseTime { get; init; }
    public double RequestsPerSecond { get; init; }
    public double ErrorRate { get; init; }
    public double ResourceUtilization { get; init; }
    public double ScalabilityScore { get; init; }
}

/// <summary>
/// Load test result
/// </summary>
public record LoadTestResult
{
    public int ConcurrentUsers { get; init; }
    public TimeSpan TestDuration { get; init; }
    public double AverageResponseTime { get; init; }
    public double Throughput { get; init; }
    public double ErrorRate { get; init; }
    public List<LoadTestDataPoint> DataPoints { get; init; } = new();
}

/// <summary>
/// Load test data point
/// </summary>
public record LoadTestDataPoint
{
    public DateTime Timestamp { get; init; }
    public int ConcurrentUsers { get; init; }
    public double ResponseTime { get; init; }
    public double Throughput { get; init; }
    public double ErrorRate { get; init; }
}

/// <summary>
/// Stress test result
/// </summary>
public record StressTestResult
{
    public int MaxConcurrentUsers { get; init; }
    public TimeSpan TimeToFailure { get; init; }
    public string FailureMode { get; init; } = string.Empty;
    public double RecoveryTime { get; init; }
    public List<string> Bottlenecks { get; init; } = new();
}

/// <summary>
/// Performance recommendation
/// </summary>
public record PerformanceRecommendation
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Effort { get; init; } = string.Empty;
    public double ExpectedImprovement { get; init; }
}

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

/// <summary>
/// Code quality request
/// </summary>
public record CodeQualityRequest
{
    public List<string> QualityMetrics { get; init; } = new();
    public double MinimumScore { get; init; }
    public List<string> CodeStandards { get; init; } = new();
    public bool IncludeSecurityScanning { get; init; }
    public bool IncludeVulnerabilityScanning { get; init; }
}

/// <summary>
/// Code quality result
/// </summary>
public record CodeQualityResult
{
    public double OverallQualityScore { get; init; }
    public Dictionary<string, double> MetricScores { get; init; } = new();
    public List<CodeIssue> Issues { get; init; } = new();
    public List<SecurityVulnerability> Vulnerabilities { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public bool MeetsStandards { get; init; }
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Code issue
/// </summary>
public record CodeIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Security vulnerability
/// </summary>
public record SecurityVulnerability
{
    public string VulnerabilityType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Mitigation { get; init; } = string.Empty;
    public double RiskScore { get; init; }
}

/// <summary>
/// Security validation request
/// </summary>
public record SecurityValidationRequest
{
    public List<string> SecurityStandards { get; init; } = new();
    public List<string> ComplianceFrameworks { get; init; } = new();
    public bool IncludePenetrationTesting { get; init; }
    public bool IncludeCodeAnalysis { get; init; }
    public Dictionary<string, object> SecurityParameters { get; init; } = new();
}

/// <summary>
/// Security validation result
/// </summary>
public record SecurityValidationResult
{
    public double SecurityScore { get; init; }
    public Dictionary<string, double> StandardScores { get; init; } = new();
    public List<SecurityIssue> Issues { get; init; } = new();
    public List<ComplianceGap> ComplianceGaps { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public bool IsCompliant { get; init; }
    public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Security issue
/// </summary>
public record SecurityIssue
{
    public string IssueType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Mitigation { get; init; } = string.Empty;
}

/// <summary>
/// Compliance gap
/// </summary>
public record ComplianceGap
{
    public string Framework { get; init; } = string.Empty;
    public string Requirement { get; init; } = string.Empty;
    public string Gap { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Remediation { get; init; } = string.Empty;
} 