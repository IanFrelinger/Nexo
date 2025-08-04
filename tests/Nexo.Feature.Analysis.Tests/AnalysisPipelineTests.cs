using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Analysis.Tests.Commands;
using Nexo.Shared.Enums;
using System;
using Nexo.Feature.Analysis.Enums;
using System.Collections.Generic;
using Nexo.Feature.Analysis.Models;
using System.Linq;

namespace Nexo.Feature.Analysis.Tests;

/// <summary>
/// Pipeline-architecture test suite for Nexo.Feature.Analysis layer.
/// Uses command classes with proper timeouts and logging to prevent hanging tests.
/// </summary>
public class AnalysisPipelineTests
{
    private readonly ILogger<AnalysisPipelineTests> _logger;

    public AnalysisPipelineTests()
    {
        _logger = NullLogger<AnalysisPipelineTests>.Instance;
    }

    [Fact(Timeout = 10000)]
    public void Analysis_Interfaces_WorkCorrectly()
    {
        _logger.LogInformation("Starting Analysis interfaces test");
        
        var command = new AnalysisValidationCommand(NullLogger<AnalysisValidationCommand>.Instance);
        var result = command.ValidateAnalysisInterfaces(timeoutMs: 5000);
        
        Assert.True(result, "Analysis interfaces should work correctly");
        _logger.LogInformation("Analysis interfaces test completed successfully");
    }

    [Fact]
    public void Analysis_Models_WorkCorrectly()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            TargetPath = "/test/path",
            AnalysisType = "code-quality",
            Options = new Dictionary<string, object> { ["option1"] = "value1" }
        };
        var issue = new AnalysisIssue
        {
            Severity = "High",
            Message = "Test issue",
            Location = "Line 42",
            Category = "Syntax"
        };
        var result = new AnalysisResult
        {
            Success = true,
            Issues = new List<AnalysisIssue> { issue },
            Metrics = new Dictionary<string, double> { ["metric1"] = 95.5 },
            Summary = "All good"
        };

        // Debug log
        _logger.LogInformation($"request.TargetPath actual value: '{request.TargetPath}'");

        // Act & Assert
        Assert.Equal("/test/path", request.TargetPath);
        Assert.Equal("code-quality", request.AnalysisType);
        Assert.True(request.Options.ContainsKey("option1"));
        Assert.Equal("value1", request.Options["option1"]);

        Assert.True(result.Success, $"Expected Success=true, got {result.Success}");
        Assert.NotNull(result.Issues);
        Assert.Single(result.Issues);
        Assert.Equal("High", result.Issues[0].Severity);
        Assert.Equal("Test issue", result.Issues[0].Message);
        Assert.Equal("Line 42", result.Issues[0].Location);
        Assert.Equal("Syntax", result.Issues[0].Category);
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.ContainsKey("metric1"));
        Assert.Equal(95.5, result.Metrics["metric1"]);
        Assert.Equal("All good", result.Summary);
    }

    [Fact(Timeout = 10000)]
    public void Analysis_Services_WorkCorrectly()
    {
        _logger.LogInformation("Starting Analysis services test");
        
        var command = new AnalysisValidationCommand(NullLogger<AnalysisValidationCommand>.Instance);
        var result = command.ValidateAnalysisServices(timeoutMs: 5000);
        
        Assert.True(result, "Analysis services should work correctly");
        _logger.LogInformation("Analysis services test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void Analysis_Enums_WorkCorrectly()
    {
        _logger.LogInformation("Starting Analysis enums test");
        
        var command = new AnalysisValidationCommand(NullLogger<AnalysisValidationCommand>.Instance);
        var result = command.ValidateAnalysisEnums(timeoutMs: 5000);
        
        Assert.True(result, "Analysis enums should work correctly");
        _logger.LogInformation("Analysis enums test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public void AnalysisStatus_EnumValues_AreDefined()
    {
        _logger.LogInformation("Starting AnalysisStatus enum validation");
        
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Pending));
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.InProgress));
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Completed));
        Assert.True(Enum.IsDefined(typeof(AnalysisStatus), AnalysisStatus.Failed));
        
        _logger.LogInformation("AnalysisStatus enum validation completed successfully");
    }

    [Fact]
    public void AnalysisRequest_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var request = new AnalysisRequest();

        // Assert
        Assert.NotNull(request);
        Assert.Equal(string.Empty, request.Code);
        Assert.Equal(string.Empty, request.TargetPath);
        Assert.Equal(string.Empty, request.AnalysisType);
        Assert.NotNull(request.Options);
        Assert.Empty(request.Options);
    }

    [Fact]
    public void AnalysisRequest_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var options = new Dictionary<string, object>
        {
            ["severity"] = "High",
            ["timeout"] = 5000,
            ["includeWarnings"] = true
        };

        // Act
        var request = new AnalysisRequest
        {
            Code = "public class Test { }",
            TargetPath = "/src/Test.cs",
            AnalysisType = "syntax-analysis",
            Options = options
        };

        // Assert
        Assert.Equal("public class Test { }", request.Code);
        Assert.Equal("/src/Test.cs", request.TargetPath);
        Assert.Equal("syntax-analysis", request.AnalysisType);
        Assert.Equal(3, request.Options.Count);
        Assert.Equal("High", request.Options["severity"]);
        Assert.Equal(5000, request.Options["timeout"]);
        Assert.True((bool)request.Options["includeWarnings"]);
    }

    [Fact]
    public void AnalysisResult_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var result = new AnalysisResult();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(string.Empty, result.Message);
        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
        Assert.NotNull(result.Metrics);
        Assert.Empty(result.Metrics);
        Assert.Equal(string.Empty, result.Summary);
    }

    [Fact]
    public void AnalysisResult_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var issues = new List<AnalysisIssue>
        {
            new AnalysisIssue { Severity = "Error", Message = "Syntax error", Location = "Line 10", Category = "Syntax" },
            new AnalysisIssue { Severity = "Warning", Message = "Unused variable", Location = "Line 15", Category = "Style" }
        };

        var metrics = new Dictionary<string, double>
        {
            ["complexity"] = 8.5,
            ["maintainability"] = 92.3,
            ["testCoverage"] = 87.1
        };

        // Act
        var result = new AnalysisResult
        {
            Success = true,
            Message = "Analysis completed successfully",
            Issues = issues,
            Metrics = metrics,
            Summary = "Code quality is good with minor issues"
        };

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Analysis completed successfully", result.Message);
        Assert.Equal(2, result.Issues.Count);
        Assert.Equal("Error", result.Issues[0].Severity);
        Assert.Equal("Warning", result.Issues[1].Severity);
        Assert.Equal(3, result.Metrics.Count);
        Assert.Equal(8.5, result.Metrics["complexity"]);
        Assert.Equal("Code quality is good with minor issues", result.Summary);
    }

    [Fact]
    public void AnalysisIssue_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var issue = new AnalysisIssue();

        // Assert
        Assert.NotNull(issue);
        Assert.Equal(string.Empty, issue.Description);
        Assert.Equal(string.Empty, issue.Severity);
        Assert.Equal(string.Empty, issue.Message);
        Assert.Equal(string.Empty, issue.Location);
        Assert.Equal(string.Empty, issue.Category);
    }

    [Fact]
    public void AnalysisIssue_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange & Act
        var issue = new AnalysisIssue
        {
            Description = "Method is too complex",
            Severity = "High",
            Message = "Consider breaking down this method into smaller functions",
            Location = "src/Controllers/UserController.cs:45",
            Category = "Complexity"
        };

        // Assert
        Assert.Equal("Method is too complex", issue.Description);
        Assert.Equal("High", issue.Severity);
        Assert.Equal("Consider breaking down this method into smaller functions", issue.Message);
        Assert.Equal("src/Controllers/UserController.cs:45", issue.Location);
        Assert.Equal("Complexity", issue.Category);
    }

    [Fact]
    public void AnalysisStatus_EnumValues_HaveCorrectOrder()
    {
        // Assert
        Assert.Equal(0, (int)AnalysisStatus.Pending);
        Assert.Equal(1, (int)AnalysisStatus.InProgress);
        Assert.Equal(2, (int)AnalysisStatus.Completed);
        Assert.Equal(3, (int)AnalysisStatus.Failed);
    }

    [Fact]
    public void AnalysisStatus_EnumValues_AreUnique()
    {
        // Arrange
        var values = Enum.GetValues(typeof(AnalysisStatus)).Cast<AnalysisStatus>().ToList();

        // Assert
        Assert.Equal(4, values.Count);
        Assert.Equal(4, values.Distinct().Count());
    }

    [Fact(Timeout = 10000)]
    public void AnalysisValidationCommand_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new AnalysisValidationCommand(null!));
        
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact(Timeout = 10000)]
    public void AnalysisValidationCommand_WithValidLogger_CreatesInstance()
    {
        // Arrange
        var logger = NullLogger<AnalysisValidationCommand>.Instance;

        // Act
        var command = new AnalysisValidationCommand(logger);

        // Assert
        Assert.NotNull(command);
    }

    [Fact]
    public void AnalysisModels_StubMethods_WorkCorrectly()
    {
        // Arrange
        var request = new AnalysisRequest { TargetPath = "/test/path", AnalysisType = "test" };
        var result = new AnalysisResult { Metrics = new Dictionary<string, double> { ["test"] = 1.0 } };
        var issue = new AnalysisIssue { Severity = "High" };

        // Act & Assert
        Assert.Equal("/test/path", request.TargetPathMethod());
        Assert.Equal("test", request.AnalysisTypeMethod());
        Assert.Equal(result.Metrics, result.MetricsMethod());
        Assert.Equal("High", issue.SeverityMethod());
    }

    [Fact]
    public void AnalysisModels_Collections_AreProperlyInitialized()
    {
        // Arrange & Act
        var request = new AnalysisRequest();
        var result = new AnalysisResult();
        var issue = new AnalysisIssue();

        // Assert
        Assert.NotNull(request.Options);
        Assert.NotNull(result.Issues);
        Assert.NotNull(result.Metrics);
        
        // Verify collections are empty but not null
        Assert.Empty(request.Options);
        Assert.Empty(result.Issues);
        Assert.Empty(result.Metrics);
    }

    [Fact]
    public void AnalysisModels_CanHandleNullCollections()
    {
        // Arrange
        var request = new AnalysisRequest { Options = null! };
        var result = new AnalysisResult { Issues = null!, Metrics = null! };

        // Act & Assert - Should not throw exceptions
        Assert.Null(request.Options);
        Assert.Null(result.Issues);
        Assert.Null(result.Metrics);
    }

    [Fact]
    public void TestResultTrends_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var trends = new TestResultTrends();

        // Assert
        Assert.NotNull(trends);
        Assert.Equal(default(DateTime), trends.StartDate);
        Assert.Equal(default(DateTime), trends.EndDate);
        Assert.NotNull(trends.SuccessRateTrend);
        Assert.NotNull(trends.ExecutionTimeTrend);
        Assert.NotNull(trends.TestCountTrend);
        Assert.NotNull(trends.FailureRateTrend);
        Assert.NotNull(trends.MemoryUsageTrend);
        Assert.NotNull(trends.CpuUsageTrend);
        Assert.NotNull(trends.EnvironmentTrends);
        Assert.NotNull(trends.ProjectTrends);
        Assert.NotNull(trends.PerformanceRegressions);
        Assert.NotNull(trends.FlakyTests);
        Assert.NotNull(trends.SeasonalPatterns);
        Assert.NotNull(trends.Recommendations);
        Assert.NotNull(trends.DailyTrends);
        Assert.Equal(0, trends.PeriodDays);
        Assert.Equal(0, trends.TotalRuns);
        Assert.Equal(0, trends.SuccessfulRuns);
        Assert.Equal(0, trends.FailedRuns);
        Assert.Equal(0.0, trends.AverageSuccessRate);
        Assert.Equal(0.0, trends.AverageExecutionTimeMs);
    }

    [Fact]
    public void TestResultTrends_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var dailyTrend = new DailyTrendPoint
        {
            Date = DateTime.Today,
            TestRuns = 100,
            SuccessRate = 95.5,
            AverageExecutionTimeMs = 1500.0,
            PassedTests = 95,
            FailedTests = 5
        };

        // Act
        var trends = new TestResultTrends
        {
            StartDate = startDate,
            EndDate = endDate,
            PeriodDays = 30,
            TotalRuns = 1000,
            SuccessfulRuns = 950,
            FailedRuns = 50,
            AverageSuccessRate = 95.0,
            AverageExecutionTimeMs = 1200.0,
            DailyTrends = new List<DailyTrendPoint> { dailyTrend }
        };

        // Assert
        Assert.Equal(startDate, trends.StartDate);
        Assert.Equal(endDate, trends.EndDate);
        Assert.Equal(30, trends.PeriodDays);
        Assert.Equal(1000, trends.TotalRuns);
        Assert.Equal(950, trends.SuccessfulRuns);
        Assert.Equal(50, trends.FailedRuns);
        Assert.Equal(95.0, trends.AverageSuccessRate);
        Assert.Equal(1200.0, trends.AverageExecutionTimeMs);
        Assert.Single(trends.DailyTrends);
        Assert.Equal(DateTime.Today, trends.DailyTrends[0].Date);
        Assert.Equal(100, trends.DailyTrends[0].TestRuns);
        Assert.Equal(95.5, trends.DailyTrends[0].SuccessRate);
    }

    [Fact]
    public void TrendData_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var trendData = new TrendData();

        // Assert
        Assert.NotNull(trendData);
        Assert.NotNull(trendData.DataPoints);
        Assert.Empty(trendData.DataPoints);
        Assert.Equal(TrendDirection.Increasing, trendData.Direction);
        Assert.Equal(0.0, trendData.Strength);
        Assert.Equal(0.0, trendData.AverageValue);
        Assert.Equal(0.0, trendData.MinValue);
        Assert.Equal(0.0, trendData.MaxValue);
        Assert.Equal(0.0, trendData.StandardDeviation);
        Assert.Equal(0.0, trendData.PercentageChange);
    }

    [Fact]
    public void TrendData_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var dataPoint = new TrendPoint
        {
            Timestamp = DateTime.UtcNow,
            Value = 85.5,
            SampleCount = 10
        };

        // Act
        var trendData = new TrendData
        {
            DataPoints = new List<TrendPoint> { dataPoint },
            Direction = TrendDirection.Increasing,
            Strength = 0.8,
            AverageValue = 85.5,
            MinValue = 80.0,
            MaxValue = 90.0,
            StandardDeviation = 2.5,
            PercentageChange = 5.2
        };

        // Assert
        Assert.Single(trendData.DataPoints);
        Assert.Equal(DateTime.UtcNow.Date, trendData.DataPoints[0].Timestamp.Date);
        Assert.Equal(85.5, trendData.DataPoints[0].Value);
        Assert.Equal(10, trendData.DataPoints[0].SampleCount);
        Assert.Equal(TrendDirection.Increasing, trendData.Direction);
        Assert.Equal(0.8, trendData.Strength);
        Assert.Equal(85.5, trendData.AverageValue);
        Assert.Equal(80.0, trendData.MinValue);
        Assert.Equal(90.0, trendData.MaxValue);
        Assert.Equal(2.5, trendData.StandardDeviation);
        Assert.Equal(5.2, trendData.PercentageChange);
    }

    [Fact]
    public void TrendDirection_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(TrendDirection), TrendDirection.Increasing));
        Assert.True(Enum.IsDefined(typeof(TrendDirection), TrendDirection.Decreasing));
        Assert.True(Enum.IsDefined(typeof(TrendDirection), TrendDirection.Stable));
        Assert.True(Enum.IsDefined(typeof(TrendDirection), TrendDirection.Fluctuating));
    }

    [Fact]
    public void PerformanceAlert_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var alert = new PerformanceAlert();

        // Assert
        Assert.NotNull(alert);
        Assert.NotEmpty(alert.AlertId);
        Assert.Equal(AlertType.HighMemoryUsage, alert.Type);
        Assert.Equal(AlertSeverity.Info, alert.Severity);
        Assert.Equal(string.Empty, alert.Title);
        Assert.Equal(string.Empty, alert.Description);
        Assert.True(alert.Timestamp > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(string.Empty, alert.Environment);
        Assert.Equal(string.Empty, alert.Project);
        Assert.Equal(string.Empty, alert.RunId);
        Assert.Equal(0.0, alert.CurrentValue);
        Assert.Equal(0.0, alert.ThresholdValue);
        Assert.Equal(string.Empty, alert.Unit);
        Assert.False(alert.IsAcknowledged);
        Assert.Null(alert.AcknowledgedAt);
        Assert.Equal(string.Empty, alert.AcknowledgedBy);
        Assert.NotNull(alert.SuggestedActions);
        Assert.Empty(alert.SuggestedActions);
        Assert.NotNull(alert.Metadata);
        Assert.Empty(alert.Metadata);
    }

    [Fact]
    public void PerformanceAlert_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var suggestedActions = new List<string> { "Restart service", "Check logs" };
        var metadata = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var alert = new PerformanceAlert
        {
            Type = AlertType.HighMemoryUsage,
            Severity = AlertSeverity.Warning,
            Title = "High Memory Usage Detected",
            Description = "Memory usage exceeded 90% threshold",
            Timestamp = timestamp,
            Environment = "Production",
            Project = "TestProject",
            RunId = "run-123",
            CurrentValue = 95.5,
            ThresholdValue = 90.0,
            Unit = "Percentage",
            SuggestedActions = suggestedActions,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(AlertType.HighMemoryUsage, alert.Type);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal("High Memory Usage Detected", alert.Title);
        Assert.Equal("Memory usage exceeded 90% threshold", alert.Description);
        Assert.Equal(timestamp, alert.Timestamp);
        Assert.Equal("Production", alert.Environment);
        Assert.Equal("TestProject", alert.Project);
        Assert.Equal("run-123", alert.RunId);
        Assert.Equal(95.5, alert.CurrentValue);
        Assert.Equal(90.0, alert.ThresholdValue);
        Assert.Equal("Percentage", alert.Unit);
        Assert.Equal(2, alert.SuggestedActions.Count);
        Assert.Equal("Restart service", alert.SuggestedActions[0]);
        Assert.Equal("Check logs", alert.SuggestedActions[1]);
        Assert.Single(alert.Metadata);
        Assert.Equal("value", alert.Metadata["key"]);
    }

    [Fact]
    public void PerformanceAlert_Acknowledge_WorksCorrectly()
    {
        // Arrange
        var alert = new PerformanceAlert();
        var acknowledgedBy = "test-user";
        var acknowledgeTime = DateTime.UtcNow;

        // Act
        alert.Acknowledge(acknowledgedBy);

        // Assert
        Assert.True(alert.IsAcknowledged);
        Assert.Equal(acknowledgedBy, alert.AcknowledgedBy);
        Assert.NotNull(alert.AcknowledgedAt);
        Assert.True(alert.AcknowledgedAt > acknowledgeTime.AddSeconds(-1));
    }

    [Fact]
    public void AlertType_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.HighMemoryUsage));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.HighCpuUsage));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.SlowTestExecution));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.HighFailureRate));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.LowSuccessRate));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.TestTimeout));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.ResourceExhaustion));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.PerformanceRegression));
        Assert.True(Enum.IsDefined(typeof(AlertType), AlertType.Custom));
    }

    [Fact]
    public void AlertSeverity_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(AlertSeverity), AlertSeverity.Info));
        Assert.True(Enum.IsDefined(typeof(AlertSeverity), AlertSeverity.Warning));
        Assert.True(Enum.IsDefined(typeof(AlertSeverity), AlertSeverity.Error));
        Assert.True(Enum.IsDefined(typeof(AlertSeverity), AlertSeverity.Critical));
    }

    [Fact]
    public void PerformanceRegression_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var regression = new PerformanceRegression();

        // Assert
        Assert.NotNull(regression);
        Assert.Equal(string.Empty, regression.Metric);
        Assert.Equal(RegressionSeverity.Minor, regression.Severity);
        Assert.Equal(default(DateTime), regression.StartDate);
        Assert.Equal(default(DateTime), regression.EndDate);
        Assert.Equal(0.0, regression.DegradationPercentage);
        Assert.NotNull(regression.AffectedTests);
        Assert.Empty(regression.AffectedTests);
        Assert.NotNull(regression.PossibleCauses);
        Assert.Empty(regression.PossibleCauses);
    }

    [Fact]
    public void PerformanceRegression_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var affectedTests = new List<string> { "Test1", "Test2" };
        var possibleCauses = new List<string> { "Memory leak", "Network latency" };

        // Act
        var regression = new PerformanceRegression
        {
            Metric = "Execution Time",
            Severity = RegressionSeverity.Major,
            StartDate = startDate,
            EndDate = endDate,
            DegradationPercentage = 25.5,
            AffectedTests = affectedTests,
            PossibleCauses = possibleCauses
        };

        // Assert
        Assert.Equal("Execution Time", regression.Metric);
        Assert.Equal(RegressionSeverity.Major, regression.Severity);
        Assert.Equal(startDate, regression.StartDate);
        Assert.Equal(endDate, regression.EndDate);
        Assert.Equal(25.5, regression.DegradationPercentage);
        Assert.Equal(2, regression.AffectedTests.Count);
        Assert.Equal("Test1", regression.AffectedTests[0]);
        Assert.Equal("Test2", regression.AffectedTests[1]);
        Assert.Equal(2, regression.PossibleCauses.Count);
        Assert.Equal("Memory leak", regression.PossibleCauses[0]);
        Assert.Equal("Network latency", regression.PossibleCauses[1]);
    }

    [Fact]
    public void RegressionSeverity_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RegressionSeverity), RegressionSeverity.Minor));
        Assert.True(Enum.IsDefined(typeof(RegressionSeverity), RegressionSeverity.Moderate));
        Assert.True(Enum.IsDefined(typeof(RegressionSeverity), RegressionSeverity.Major));
        Assert.True(Enum.IsDefined(typeof(RegressionSeverity), RegressionSeverity.Critical));
    }

    [Fact]
    public void FlakyTestAnalysis_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var flakyTest = new FlakyTestAnalysis();

        // Assert
        Assert.NotNull(flakyTest);
        Assert.Equal(string.Empty, flakyTest.TestName);
        Assert.Equal(0.0, flakyTest.FlakinessPercentage);
        Assert.Equal(0, flakyTest.TotalRuns);
        Assert.Equal(0, flakyTest.InconsistentRuns);
        Assert.NotNull(flakyTest.FailurePatterns);
        Assert.Empty(flakyTest.FailurePatterns);
        Assert.NotNull(flakyTest.SuggestedFixes);
        Assert.Empty(flakyTest.SuggestedFixes);
    }

    [Fact]
    public void FlakyTestAnalysis_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var failurePatterns = new List<string> { "Timeout", "Network error" };
        var suggestedFixes = new List<string> { "Increase timeout", "Add retry logic" };

        // Act
        var flakyTest = new FlakyTestAnalysis
        {
            TestName = "IntegrationTest",
            FlakinessPercentage = 15.5,
            TotalRuns = 100,
            InconsistentRuns = 15,
            FailurePatterns = failurePatterns,
            SuggestedFixes = suggestedFixes
        };

        // Assert
        Assert.Equal("IntegrationTest", flakyTest.TestName);
        Assert.Equal(15.5, flakyTest.FlakinessPercentage);
        Assert.Equal(100, flakyTest.TotalRuns);
        Assert.Equal(15, flakyTest.InconsistentRuns);
        Assert.Equal(2, flakyTest.FailurePatterns.Count);
        Assert.Equal("Timeout", flakyTest.FailurePatterns[0]);
        Assert.Equal("Network error", flakyTest.FailurePatterns[1]);
        Assert.Equal(2, flakyTest.SuggestedFixes.Count);
        Assert.Equal("Increase timeout", flakyTest.SuggestedFixes[0]);
        Assert.Equal("Add retry logic", flakyTest.SuggestedFixes[1]);
    }

    [Fact]
    public void SeasonalPattern_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var pattern = new SeasonalPattern();

        // Assert
        Assert.NotNull(pattern);
        Assert.Equal(SeasonalPatternType.Daily, pattern.Type);
        Assert.Equal(string.Empty, pattern.Description);
        Assert.Equal(0.0, pattern.Strength);
        Assert.NotNull(pattern.AffectedMetrics);
        Assert.Empty(pattern.AffectedMetrics);
        Assert.NotNull(pattern.TimeRange);
    }

    [Fact]
    public void SeasonalPattern_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var affectedMetrics = new List<string> { "Success Rate", "Execution Time" };
        var timeRange = new TimeRange
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday },
            Months = new List<int> { 1, 2, 3 }
        };

        // Act
        var pattern = new SeasonalPattern
        {
            Type = SeasonalPatternType.Weekly,
            Description = "Performance degrades on Mondays",
            Strength = 0.75,
            AffectedMetrics = affectedMetrics,
            TimeRange = timeRange
        };

        // Assert
        Assert.Equal(SeasonalPatternType.Weekly, pattern.Type);
        Assert.Equal("Performance degrades on Mondays", pattern.Description);
        Assert.Equal(0.75, pattern.Strength);
        Assert.Equal(2, pattern.AffectedMetrics.Count);
        Assert.Equal("Success Rate", pattern.AffectedMetrics[0]);
        Assert.Equal("Execution Time", pattern.AffectedMetrics[1]);
        Assert.Equal(TimeSpan.FromHours(9), pattern.TimeRange.StartTime);
        Assert.Equal(TimeSpan.FromHours(17), pattern.TimeRange.EndTime);
        Assert.Equal(2, pattern.TimeRange.DaysOfWeek.Count);
        Assert.Equal(DayOfWeek.Monday, pattern.TimeRange.DaysOfWeek[0]);
        Assert.Equal(DayOfWeek.Friday, pattern.TimeRange.DaysOfWeek[1]);
        Assert.Equal(3, pattern.TimeRange.Months.Count);
        Assert.Equal(1, pattern.TimeRange.Months[0]);
        Assert.Equal(2, pattern.TimeRange.Months[1]);
        Assert.Equal(3, pattern.TimeRange.Months[2]);
    }

    [Fact]
    public void SeasonalPatternType_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(SeasonalPatternType), SeasonalPatternType.Daily));
        Assert.True(Enum.IsDefined(typeof(SeasonalPatternType), SeasonalPatternType.Weekly));
        Assert.True(Enum.IsDefined(typeof(SeasonalPatternType), SeasonalPatternType.Monthly));
        Assert.True(Enum.IsDefined(typeof(SeasonalPatternType), SeasonalPatternType.Quarterly));
        Assert.True(Enum.IsDefined(typeof(SeasonalPatternType), SeasonalPatternType.Yearly));
    }

    [Fact]
    public void TrendRecommendation_WithEmptyValues_InitializesCorrectly()
    {
        // Arrange & Act
        var recommendation = new TrendRecommendation();

        // Assert
        Assert.NotNull(recommendation);
        Assert.Equal(RecommendationType.PerformanceOptimization, recommendation.Type);
        Assert.Equal(string.Empty, recommendation.Title);
        Assert.Equal(string.Empty, recommendation.Description);
        Assert.Equal(RecommendationPriority.Low, recommendation.Priority);
        Assert.Equal(string.Empty, recommendation.EstimatedImpact);
        Assert.NotNull(recommendation.SuggestedActions);
        Assert.Empty(recommendation.SuggestedActions);
    }

    [Fact]
    public void TrendRecommendation_WithValidData_PropertiesSetCorrectly()
    {
        // Arrange
        var suggestedActions = new List<string> { "Optimize database queries", "Add caching" };

        // Act
        var recommendation = new TrendRecommendation
        {
            Type = RecommendationType.PerformanceOptimization,
            Title = "Database Performance Issue",
            Description = "Database queries are taking too long",
            Priority = RecommendationPriority.High,
            EstimatedImpact = "30% performance improvement",
            SuggestedActions = suggestedActions
        };

        // Assert
        Assert.Equal(RecommendationType.PerformanceOptimization, recommendation.Type);
        Assert.Equal("Database Performance Issue", recommendation.Title);
        Assert.Equal("Database queries are taking too long", recommendation.Description);
        Assert.Equal(RecommendationPriority.High, recommendation.Priority);
        Assert.Equal("30% performance improvement", recommendation.EstimatedImpact);
        Assert.Equal(2, recommendation.SuggestedActions.Count);
        Assert.Equal("Optimize database queries", recommendation.SuggestedActions[0]);
        Assert.Equal("Add caching", recommendation.SuggestedActions[1]);
    }

    [Fact]
    public void RecommendationType_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RecommendationType), RecommendationType.PerformanceOptimization));
        Assert.True(Enum.IsDefined(typeof(RecommendationType), RecommendationType.TestStability));
        Assert.True(Enum.IsDefined(typeof(RecommendationType), RecommendationType.Infrastructure));
        Assert.True(Enum.IsDefined(typeof(RecommendationType), RecommendationType.ProcessImprovement));
        Assert.True(Enum.IsDefined(typeof(RecommendationType), RecommendationType.MonitoringEnhancement));
    }

    [Fact]
    public void RecommendationPriority_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RecommendationPriority), RecommendationPriority.Low));
        Assert.True(Enum.IsDefined(typeof(RecommendationPriority), RecommendationPriority.Medium));
        Assert.True(Enum.IsDefined(typeof(RecommendationPriority), RecommendationPriority.High));
        Assert.True(Enum.IsDefined(typeof(RecommendationPriority), RecommendationPriority.Critical));
    }

    [Fact]
    public void CommandPriority_EnumValues_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(Nexo.Feature.Pipeline.Enums.CommandPriority), Nexo.Feature.Pipeline.Enums.CommandPriority.Critical));
        Assert.True(Enum.IsDefined(typeof(Nexo.Feature.Pipeline.Enums.CommandPriority), Nexo.Feature.Pipeline.Enums.CommandPriority.High));
        Assert.True(Enum.IsDefined(typeof(Nexo.Feature.Pipeline.Enums.CommandPriority), Nexo.Feature.Pipeline.Enums.CommandPriority.Normal));
        Assert.True(Enum.IsDefined(typeof(Nexo.Feature.Pipeline.Enums.CommandPriority), Nexo.Feature.Pipeline.Enums.CommandPriority.Low));
        Assert.True(Enum.IsDefined(typeof(Nexo.Feature.Pipeline.Enums.CommandPriority), Nexo.Feature.Pipeline.Enums.CommandPriority.Background));
    }

    [Fact]
    public void CommandPriority_EnumValues_HaveCorrectOrder()
    {
        // Assert
        Assert.Equal(0, (int)Nexo.Feature.Pipeline.Enums.CommandPriority.Critical);
        Assert.Equal(1, (int)Nexo.Feature.Pipeline.Enums.CommandPriority.High);
        Assert.Equal(2, (int)Nexo.Feature.Pipeline.Enums.CommandPriority.Normal);
        Assert.Equal(3, (int)Nexo.Feature.Pipeline.Enums.CommandPriority.Low);
        Assert.Equal(4, (int)Nexo.Feature.Pipeline.Enums.CommandPriority.Background);
    }

    [Fact]
    public void AnalysisModels_Integration_WorkTogether()
    {
        // Arrange
        var request = new AnalysisRequest
        {
            TargetPath = "/test/path",
            AnalysisType = "performance",
            Options = new Dictionary<string, object> { ["timeout"] = 5000 }
        };

        var issue = new AnalysisIssue
        {
            Severity = "High",
            Message = "Performance regression detected",
            Location = "TestSuite.cs:42",
            Category = "Performance"
        };

        var result = new AnalysisResult
        {
            Success = false,
            Issues = new List<AnalysisIssue> { issue },
            Metrics = new Dictionary<string, double> { ["executionTime"] = 2500.0 }
        };

        var alert = new PerformanceAlert
        {
            Type = AlertType.PerformanceRegression,
            Severity = AlertSeverity.Warning,
            Title = "Performance Regression",
            Description = "Test execution time increased by 25%",
            CurrentValue = 2500.0,
            ThresholdValue = 2000.0
        };

        // Act & Assert
        Assert.Equal("/test/path", request.TargetPath);
        Assert.Equal("performance", request.AnalysisType);
        Assert.Equal(5000, request.Options["timeout"]);

        Assert.False(result.Success);
        Assert.Single(result.Issues);
        Assert.Equal("High", result.Issues[0].Severity);
        Assert.Equal("Performance regression detected", result.Issues[0].Message);
        Assert.Equal(2500.0, result.Metrics["executionTime"]);

        Assert.Equal(AlertType.PerformanceRegression, alert.Type);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal("Performance Regression", alert.Title);
        Assert.Equal(2500.0, alert.CurrentValue);
        Assert.Equal(2000.0, alert.ThresholdValue);
    }

    [Fact]
    public void AnalysisModels_Collections_AreProperlyInitializedInComplexModels()
    {
        // Arrange & Act
        var trends = new TestResultTrends();
        var alert = new PerformanceAlert();
        var regression = new PerformanceRegression();
        var flakyTest = new FlakyTestAnalysis();
        var pattern = new SeasonalPattern();
        var recommendation = new TrendRecommendation();

        // Assert
        Assert.NotNull(trends.EnvironmentTrends);
        Assert.NotNull(trends.ProjectTrends);
        Assert.NotNull(trends.PerformanceRegressions);
        Assert.NotNull(trends.FlakyTests);
        Assert.NotNull(trends.SeasonalPatterns);
        Assert.NotNull(trends.Recommendations);
        Assert.NotNull(trends.DailyTrends);

        Assert.NotNull(alert.SuggestedActions);
        Assert.NotNull(alert.Metadata);

        Assert.NotNull(regression.AffectedTests);
        Assert.NotNull(regression.PossibleCauses);

        Assert.NotNull(flakyTest.FailurePatterns);
        Assert.NotNull(flakyTest.SuggestedFixes);

        Assert.NotNull(pattern.AffectedMetrics);
        Assert.NotNull(pattern.TimeRange);
        Assert.NotNull(pattern.TimeRange.DaysOfWeek);
        Assert.NotNull(pattern.TimeRange.Months);

        Assert.NotNull(recommendation.SuggestedActions);

        // Verify collections are empty but not null
        Assert.Empty(trends.EnvironmentTrends);
        Assert.Empty(trends.ProjectTrends);
        Assert.Empty(trends.PerformanceRegressions);
        Assert.Empty(trends.FlakyTests);
        Assert.Empty(trends.SeasonalPatterns);
        Assert.Empty(trends.Recommendations);
        Assert.Empty(trends.DailyTrends);

        Assert.Empty(alert.SuggestedActions);
        Assert.Empty(alert.Metadata);

        Assert.Empty(regression.AffectedTests);
        Assert.Empty(regression.PossibleCauses);

        Assert.Empty(flakyTest.FailurePatterns);
        Assert.Empty(flakyTest.SuggestedFixes);

        Assert.Empty(pattern.AffectedMetrics);
        Assert.Empty(pattern.TimeRange.DaysOfWeek);
        Assert.Empty(pattern.TimeRange.Months);

        Assert.Empty(recommendation.SuggestedActions);
    }
} 