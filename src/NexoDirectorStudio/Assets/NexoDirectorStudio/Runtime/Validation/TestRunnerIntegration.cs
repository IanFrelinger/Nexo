using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validation;

namespace NexoDirectorStudio.Validation
{
    /// <summary>
    /// Integration with Unity's test runner for component validation.
    /// Provides a programmatic way to run tests and get results.
    /// </summary>
    public class TestRunnerIntegration
    {
        private readonly ComponentValidator _validator;
        private readonly int _maxRetries;
        private readonly float _timeoutSeconds;

        public TestRunnerIntegration(int maxRetries = 3, float timeoutSeconds = 30f)
        {
            _maxRetries = maxRetries;
            _timeoutSeconds = timeoutSeconds;
            _validator = new ComponentValidator(maxRetries, timeoutSeconds);
        }

        /// <summary>
        /// Runs component validation tests and returns detailed results.
        /// </summary>
        public async Task<ComponentValidationResult> RunValidationTestsAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            CancellationToken cancellationToken = default)
        {
            Debug.Log($"🧪 Starting component validation for {contentBundle.Assets.Count} assets");
            
            var result = await _validator.ValidateAsync(contentBundle, gamePlan, cancellationToken);
            
            LogValidationResults(result);
            
            return result;
        }

        /// <summary>
        /// Runs validation tests with retry logic until they pass or max retries reached.
        /// </summary>
        public async Task<ComponentValidationResult> RunValidationTestsWithRetryAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            Func<ComponentValidationResult, Task<ContentBundle>> regenerateCallback,
            CancellationToken cancellationToken = default)
        {
            Debug.Log($"🔄 Starting component validation with retry logic (max {_maxRetries} attempts)");
            
            var result = await _validator.ValidateWithRetryAsync(
                contentBundle, 
                gamePlan, 
                regenerateCallback, 
                cancellationToken);
            
            LogValidationResults(result);
            
            return result;
        }

        /// <summary>
        /// Runs specific test suites for targeted validation.
        /// </summary>
        public async Task<ComponentValidationResult> RunTargetedValidationAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            string[] testSuiteNames,
            CancellationToken cancellationToken = default)
        {
            Debug.Log($"🎯 Running targeted validation for test suites: {string.Join(", ", testSuiteNames)}");
            
            // Create a custom validator with only the specified test suites
            var customValidator = new ComponentValidator(_maxRetries, _timeoutSeconds);
            
            var result = await customValidator.ValidateAsync(contentBundle, gamePlan, cancellationToken);
            
            // Filter results to only include the requested test suites
            result.TestSuiteResults = result.TestSuiteResults
                .Where(r => testSuiteNames.Contains(r.TestSuiteName))
                .ToList();
            
            LogValidationResults(result);
            
            return result;
        }

        /// <summary>
        /// Runs performance-focused validation tests.
        /// </summary>
        public async Task<ComponentValidationResult> RunPerformanceValidationAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            CancellationToken cancellationToken = default)
        {
            return await RunTargetedValidationAsync(
                contentBundle, 
                gamePlan, 
                new[] { "PerformanceTestSuite" }, 
                cancellationToken);
        }

        /// <summary>
        /// Runs accessibility-focused validation tests.
        /// </summary>
        public async Task<ComponentValidationResult> RunAccessibilityValidationAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            CancellationToken cancellationToken = default)
        {
            return await RunTargetedValidationAsync(
                contentBundle, 
                gamePlan, 
                new[] { "AccessibilityTestSuite" }, 
                cancellationToken);
        }

        /// <summary>
        /// Runs integration-focused validation tests.
        /// </summary>
        public async Task<ComponentValidationResult> RunIntegrationValidationAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            CancellationToken cancellationToken = default)
        {
            return await RunTargetedValidationAsync(
                contentBundle, 
                gamePlan, 
                new[] { "IntegrationTestSuite" }, 
                cancellationToken);
        }

        /// <summary>
        /// Runs genre-specific validation tests.
        /// </summary>
        public async Task<ComponentValidationResult> RunGenreValidationAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            CancellationToken cancellationToken = default)
        {
            return await RunTargetedValidationAsync(
                contentBundle, 
                gamePlan, 
                new[] { "GenreSpecificTestSuite" }, 
                cancellationToken);
        }

        /// <summary>
        /// Runs functionality-focused validation tests.
        /// </summary>
        public async Task<ComponentValidationResult> RunFunctionalityValidationAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            CancellationToken cancellationToken = default)
        {
            return await RunTargetedValidationAsync(
                contentBundle, 
                gamePlan, 
                new[] { "FunctionalityTestSuite" }, 
                cancellationToken);
        }

        /// <summary>
        /// Generates a detailed validation report.
        /// </summary>
        public string GenerateValidationReport(ComponentValidationResult result)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== COMPONENT VALIDATION REPORT ===");
            report.AppendLine($"Content Bundle ID: {result.ContentBundleId}");
            report.AppendLine($"Game Plan ID: {result.GamePlanId}");
            report.AppendLine($"Validation Duration: {result.ValidationDuration.TotalSeconds:F2}s");
            report.AppendLine($"Overall Result: {(result.OverallPassed ? "PASSED" : "FAILED")}");
            report.AppendLine($"Overall Score: {result.OverallScore:F1}%");
            report.AppendLine($"Total Tests: {result.TotalTests}");
            report.AppendLine($"Passed Tests: {result.PassedTests}");
            report.AppendLine($"Failed Tests: {result.FailedTests}");
            report.AppendLine();
            
            if (!string.IsNullOrEmpty(result.ValidationError))
            {
                report.AppendLine($"Validation Error: {result.ValidationError}");
                report.AppendLine();
            }
            
            report.AppendLine("=== TEST SUITE RESULTS ===");
            foreach (var suiteResult in result.TestSuiteResults)
            {
                report.AppendLine($"\n{suiteResult.TestSuiteName}:");
                report.AppendLine($"  Status: {(suiteResult.Passed ? "PASSED" : "FAILED")}");
                report.AppendLine($"  Duration: {suiteResult.Duration.TotalSeconds:F2}s");
                report.AppendLine($"  Tests: {suiteResult.PassedTests}/{suiteResult.TotalTests} passed");
                
                if (!string.IsNullOrEmpty(suiteResult.ErrorMessage))
                {
                    report.AppendLine($"  Error: {suiteResult.ErrorMessage}");
                }
                
                report.AppendLine("  Individual Tests:");
                foreach (var testResult in suiteResult.TestResults)
                {
                    var status = testResult.Passed ? "✓" : "✗";
                    report.AppendLine($"    {status} {testResult.TestName} ({testResult.Duration.TotalMilliseconds:F0}ms)");
                    if (!string.IsNullOrEmpty(testResult.ErrorMessage))
                    {
                        report.AppendLine($"      Error: {testResult.ErrorMessage}");
                    }
                }
            }
            
            return report.ToString();
        }

        /// <summary>
        /// Generates a summary validation report for quick overview.
        /// </summary>
        public string GenerateValidationSummary(ComponentValidationResult result)
        {
            var summary = new System.Text.StringBuilder();
            
            summary.AppendLine("=== VALIDATION SUMMARY ===");
            summary.AppendLine($"Status: {(result.OverallPassed ? "✅ PASSED" : "❌ FAILED")}");
            summary.AppendLine($"Score: {result.OverallScore:F1}% ({result.PassedTests}/{result.TotalTests} tests)");
            summary.AppendLine($"Duration: {result.ValidationDuration.TotalSeconds:F2}s");
            
            if (!result.OverallPassed)
            {
                summary.AppendLine("\nFailed Test Suites:");
                foreach (var suiteResult in result.TestSuiteResults.Where(r => !r.Passed))
                {
                    summary.AppendLine($"  ❌ {suiteResult.TestSuiteName} ({suiteResult.FailedTests} failures)");
                }
            }
            
            return summary.ToString();
        }

        /// <summary>
        /// Exports validation results to a file.
        /// </summary>
        public async Task ExportValidationReportAsync(
            ComponentValidationResult result, 
            string filePath, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var report = GenerateValidationReport(result);
                await System.IO.File.WriteAllTextAsync(filePath, report, cancellationToken);
                Debug.Log($"📄 Validation report exported to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export validation report: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if validation results meet quality thresholds.
        /// </summary>
        public bool MeetsQualityThresholds(ComponentValidationResult result, float minScore = 80f)
        {
            return result.OverallPassed && result.OverallScore >= minScore;
        }

        /// <summary>
        /// Gets validation recommendations based on test results.
        /// </summary>
        public List<string> GetValidationRecommendations(ComponentValidationResult result)
        {
            var recommendations = new List<string>();
            
            if (result.OverallScore < 80f)
            {
                recommendations.Add("Overall score is below 80%. Consider regenerating components with improved quality.");
            }
            
            var failedSuites = result.TestSuiteResults.Where(r => !r.Passed).ToList();
            foreach (var suite in failedSuites)
            {
                switch (suite.TestSuiteName)
                {
                    case "FunctionalityTestSuite":
                        recommendations.Add("Functionality issues detected. Check component instantiation and basic interactions.");
                        break;
                    case "PerformanceTestSuite":
                        recommendations.Add("Performance issues detected. Consider reducing component count or complexity.");
                        break;
                    case "IntegrationTestSuite":
                        recommendations.Add("Integration issues detected. Check component interactions and scene hierarchy.");
                        break;
                    case "AccessibilityTestSuite":
                        recommendations.Add("Accessibility issues detected. Improve color contrast and text readability.");
                        break;
                    case "GenreSpecificTestSuite":
                        recommendations.Add("Genre-specific issues detected. Ensure components match genre requirements.");
                        break;
                }
            }
            
            return recommendations;
        }

        private void LogValidationResults(ComponentValidationResult result)
        {
            if (result.OverallPassed)
            {
                Debug.Log($"✅ Component validation PASSED - Score: {result.OverallScore:F1}% ({result.PassedTests}/{result.TotalTests} tests)");
            }
            else
            {
                Debug.LogError($"❌ Component validation FAILED - Score: {result.OverallScore:F1}% ({result.PassedTests}/{result.TotalTests} tests)");
                Debug.LogError($"Validation Error: {result.ValidationError}");
                
                // Log failed test suites
                var failedSuites = result.TestSuiteResults.Where(r => !r.Passed).ToList();
                foreach (var suite in failedSuites)
                {
                    Debug.LogError($"  ❌ {suite.TestSuiteName}: {suite.FailedTests} failures");
                }
            }
        }
    }
}
