using System;
using System.Collections.Generic;
using System.Linq;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validation
{
    /// <summary>
    /// Represents the result of a single component validation test.
    /// </summary>
    public sealed record ComponentValidationResult
    {
        public string TestName { get; init; } = string.Empty;
        public bool Passed { get; init; }
        public string Message { get; init; } = string.Empty;
        public float Score { get; init; } = 0f; // Score from 0-100
        public IReadOnlyList<string> Issues { get; init; } = new List<string>();
        public IReadOnlyList<string> Suggestions { get; init; } = new List<string>();
    }

    /// <summary>
    /// Aggregates results from multiple component validation tests.
    /// </summary>
    public sealed record OverallValidationResult
    {
        public string ReportId { get; init; } = System.Guid.NewGuid().ToString();
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public IReadOnlyList<ComponentValidationResult> Results { get; init; } = new List<ComponentValidationResult>();
        public bool OverallPassed => Results.All(r => r.Passed);
        public float OverallScore => Results.Any() ? Results.Average(r => r.Score) : 0f;
    }

    /// <summary>
    /// Interface for a component validator.
    /// </summary>
    public interface IComponentValidator
    {
        string Name { get; }
        Task<ComponentValidationResult> ValidateAsync(ContentBundle content, GamePlan plan, CancellationToken ct);
    }

    /// <summary>
    /// Base class for component validators.
    /// </summary>
    public abstract class BaseComponentValidator : IComponentValidator
    {
        public abstract string Name { get; }
        public abstract Task<ComponentValidationResult> ValidateAsync(ContentBundle content, GamePlan plan, CancellationToken ct);

        protected ComponentValidationResult Pass(string message, float score = 100f)
        {
            return new ComponentValidationResult { TestName = Name, Passed = true, Message = message, Score = score };
        }

        protected ComponentValidationResult Fail(string message, float score = 0f, IEnumerable<string>? issues = null, IEnumerable<string>? suggestions = null)
        {
            return new ComponentValidationResult
            {
                TestName = Name,
                Passed = false,
                Message = message,
                Score = score,
                Issues = issues?.ToList() ?? new List<string>(),
                Suggestions = suggestions?.ToList() ?? new List<string>()
            };
        }
    }

    /// <summary>
    /// Interface for component test suites.
    /// </summary>
    public interface IComponentTest
    {
        string Name { get; }
        Task<TestSuiteResult> RunTestsAsync(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Result of a test suite execution.
    /// </summary>
    public class TestSuiteResult
    {
        public string TestSuiteName { get; set; } = "";
        public bool Passed { get; set; }
        public List<TestResult> TestResults { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; } = "";
        public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    }

    /// <summary>
    /// Result of an individual test.
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Passed { get; set; }
        public string Message { get; set; } = "";
        public float Score { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    /// <summary>
    /// Main validation result containing all test suite results.
    /// </summary>
    public class ComponentValidationResult
    {
        public string ContentBundleId { get; set; } = "";
        public string GamePlanId { get; set; } = "";
        public DateTimeOffset ValidationStartTime { get; set; }
        public DateTimeOffset ValidationEndTime { get; set; }
        public TimeSpan TotalDuration => ValidationEndTime - ValidationStartTime;
        public List<TestSuiteResult> TestSuiteResults { get; set; } = new();
        public bool OverallPassed => TestSuiteResults.All(r => r.Passed);
        public float OverallScore => TestSuiteResults.Any() ? TestSuiteResults.Average(r => r.Passed ? 100f : 0f) : 0f;
        public int TotalTests => TestSuiteResults.Sum(r => r.TestResults.Count);
        public int PassedTests => TestSuiteResults.Sum(r => r.TestResults.Count(t => t.Passed));
        public int FailedTests => TotalTests - PassedTests;
        public string ValidationError { get; set; } = "";
        public int MaxRetries { get; set; }
        public int RetryCount { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }
}
