using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validation
{
    /// <summary>
    /// Validates generated game components using Unity's test runner.
    /// Ensures components are working correctly before promoting them to the main pipeline.
    /// </summary>
    public class ComponentValidator
    {
        private readonly List<IComponentTest> _testSuites;
        private readonly int _maxRetries;
        private readonly float _timeoutSeconds;

        public ComponentValidator(int maxRetries = 3, float timeoutSeconds = 30f)
        {
            _maxRetries = maxRetries;
            _timeoutSeconds = timeoutSeconds;
            _testSuites = new List<IComponentTest>
            {
                new FunctionalityTestSuite(),
                new PerformanceTestSuite(),
                new IntegrationTestSuite(),
                new AccessibilityTestSuite(),
                new GenreSpecificTestSuite()
            };
        }

        /// <summary>
        /// Validates a content bundle and its generated components.
        /// Returns validation result with detailed test results.
        /// </summary>
        public async Task<ComponentValidationResult> ValidateAsync(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken = default)
        {
            var result = new ComponentValidationResult
            {
                ContentBundleId = contentBundle.Id,
                GamePlanId = gamePlan.Id,
                ValidationStartTime = DateTimeOffset.UtcNow,
                MaxRetries = _maxRetries
            };

            try
            {
                Debug.Log($"🧪 Starting component validation for {contentBundle.Id}...");

                // Run all test suites
                var testSuiteResults = new List<TestSuiteResult>();
                foreach (var testSuite in _testSuites)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    Debug.Log($"Running {testSuite.Name}...");
                    var testSuiteResult = await testSuite.RunTestsAsync(contentBundle, gamePlan, cancellationToken);
                    testSuiteResults.Add(testSuiteResult);
                }

                result.TestSuiteResults = testSuiteResults;
                result.ValidationEndTime = DateTimeOffset.UtcNow;

                // Generate recommendations
                result.Recommendations = GenerateRecommendations(result);

                Debug.Log($"✅ Component validation completed - Score: {result.OverallScore:F1}% ({result.PassedTests}/{result.TotalTests} tests passed)");
            }
            catch (Exception ex)
            {
                result.ValidationEndTime = DateTimeOffset.UtcNow;
                result.ValidationError = ex.Message;
                Debug.LogError($"❌ Component validation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates components with retry logic for failed validations.
        /// </summary>
        public async Task<ComponentValidationResult> ValidateWithRetryAsync(
            ContentBundle contentBundle, 
            GamePlan gamePlan, 
            Func<ComponentValidationResult, Task<ContentBundle>> regenerateContentFunc,
            CancellationToken cancellationToken = default)
        {
            ComponentValidationResult lastResult = null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                Debug.Log($"🔄 Validation attempt {attempt + 1}/{_maxRetries + 1}...");

                lastResult = await ValidateAsync(contentBundle, gamePlan, cancellationToken);
                lastResult.RetryCount = attempt;

                if (lastResult.OverallPassed)
                {
                    Debug.Log($"✅ Validation passed on attempt {attempt + 1}.");
                    return lastResult;
                }

                if (attempt < _maxRetries)
                {
                    Debug.LogWarning($"⚠️ Validation failed on attempt {attempt + 1}. Regenerating content...");
                    try
                    {
                        contentBundle = await regenerateContentFunc(lastResult);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Failed to regenerate content: {ex.Message}");
                        break;
                    }
                }
            }

            Debug.LogError($"❌ Validation failed after {_maxRetries + 1} attempts.");
            return lastResult;
        }

        /// <summary>
        /// Gets validation recommendations based on test results.
        /// </summary>
        public List<string> GetValidationRecommendations(ComponentValidationResult result)
        {
            var recommendations = new List<string>();

            foreach (var testSuiteResult in result.TestSuiteResults)
            {
                if (!testSuiteResult.Passed)
                {
                    recommendations.Add($"Test suite '{testSuiteResult.TestSuiteName}' failed");
                    
                    foreach (var testResult in testSuiteResult.TestResults.Where(t => !t.Passed))
                    {
                        recommendations.Add($"  - {testResult.TestName}: {testResult.Message}");
                        
                        foreach (var issue in testResult.Issues)
                        {
                            recommendations.Add($"    Issue: {issue}");
                        }
                        
                        foreach (var suggestion in testResult.Suggestions)
                        {
                            recommendations.Add($"    Suggestion: {suggestion}");
                        }
                    }
                }
            }

            return recommendations;
        }

        /// <summary>
        /// Exports validation report to a file.
        /// </summary>
        public async Task ExportValidationReportAsync(ComponentValidationResult result, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var reportContent = GenerateValidationReport(result);
                await System.IO.File.WriteAllTextAsync(filePath, reportContent, cancellationToken);
                Debug.Log($"📄 Validation report exported to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to export validation report: {ex.Message}");
            }
        }

        private List<string> GenerateRecommendations(ComponentValidationResult result)
        {
            var recommendations = new List<string>();

            if (!result.OverallPassed)
            {
                recommendations.Add("Overall validation failed - review all test results");
            }

            if (result.OverallScore < 80f)
            {
                recommendations.Add("Overall score is below 80% - consider improving component quality");
            }

            var failedTestSuites = result.TestSuiteResults.Where(r => !r.Passed).ToList();
            if (failedTestSuites.Any())
            {
                recommendations.Add($"Failed test suites: {string.Join(", ", failedTestSuites.Select(r => r.TestSuiteName))}");
            }

            var highFailureRate = (float)result.FailedTests / result.TotalTests > 0.3f;
            if (highFailureRate)
            {
                recommendations.Add("High failure rate detected - consider regenerating components");
            }

            return recommendations;
        }

        private string GenerateValidationReport(ComponentValidationResult result)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== COMPONENT VALIDATION REPORT ===");
            report.AppendLine($"Content Bundle ID: {result.ContentBundleId}");
            report.AppendLine($"Game Plan ID: {result.GamePlanId}");
            report.AppendLine($"Validation Start: {result.ValidationStartTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Validation End: {result.ValidationEndTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total Duration: {result.TotalDuration.TotalSeconds:F2}s");
            report.AppendLine($"Overall Passed: {(result.OverallPassed ? "✅ YES" : "❌ NO")}");
            report.AppendLine($"Overall Score: {result.OverallScore:F1}%");
            report.AppendLine($"Total Tests: {result.TotalTests}");
            report.AppendLine($"Passed Tests: {result.PassedTests}");
            report.AppendLine($"Failed Tests: {result.FailedTests}");
            report.AppendLine($"Max Retries: {result.MaxRetries}");
            report.AppendLine($"Retry Count: {result.RetryCount}");
            
            if (!string.IsNullOrEmpty(result.ValidationError))
            {
                report.AppendLine($"Validation Error: {result.ValidationError}");
            }

            report.AppendLine("\n=== TEST SUITE RESULTS ===");
            foreach (var testSuiteResult in result.TestSuiteResults)
            {
                report.AppendLine($"\n{testSuiteResult.TestSuiteName}:");
                report.AppendLine($"  Passed: {(testSuiteResult.Passed ? "✅ YES" : "❌ NO")}");
                report.AppendLine($"  Duration: {testSuiteResult.Duration.TotalSeconds:F2}s");
                report.AppendLine($"  Tests: {testSuiteResult.TestResults.Count}");
                
                if (!string.IsNullOrEmpty(testSuiteResult.ErrorMessage))
                {
                    report.AppendLine($"  Error: {testSuiteResult.ErrorMessage}");
                }

                foreach (var testResult in testSuiteResult.TestResults)
                {
                    report.AppendLine($"    {testResult.TestName}: {(testResult.Passed ? "✅" : "❌")} {testResult.Message} (Score: {testResult.Score:F1}%)");
                    
                    if (testResult.Issues.Any())
                    {
                        report.AppendLine("      Issues:");
                        foreach (var issue in testResult.Issues)
                        {
                            report.AppendLine($"        - {issue}");
                        }
                    }
                    
                    if (testResult.Suggestions.Any())
                    {
                        report.AppendLine("      Suggestions:");
                        foreach (var suggestion in testResult.Suggestions)
                        {
                            report.AppendLine($"        - {suggestion}");
                        }
                    }
                }
            }

            if (result.Recommendations.Any())
            {
                report.AppendLine("\n=== RECOMMENDATIONS ===");
                foreach (var recommendation in result.Recommendations)
                {
                    report.AppendLine($"- {recommendation}");
                }
            }

            return report.ToString();
        }
    }
}
