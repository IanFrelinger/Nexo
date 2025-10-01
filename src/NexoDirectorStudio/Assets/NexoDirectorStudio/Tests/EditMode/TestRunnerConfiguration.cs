using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Test runner configuration and utilities for running different test suites.
    /// Provides easy setup and execution of various test categories.
    /// </summary>
    public static class TestRunnerConfiguration
    {
        /// <summary>
        /// Runs a specific test category.
        /// </summary>
        public static async Task<TestExecutionResult> RunTestCategoryAsync(string category, string environment = "Development")
        {
            var config = TestSuiteConfigs.All.GetValueOrDefault(environment, TestSuiteConfigs.All["Development"]);
            var result = new TestExecutionResult
            {
                Category = category,
                Environment = environment,
                StartTime = DateTimeOffset.UtcNow
            };

            try
            {
                switch (category.ToLower())
                {
                    case "e2e":
                        result = await TestExecutionEngine.RunE2ETestsAsync(config);
                        break;
                    case "integration":
                        result = await TestExecutionEngine.RunIntegrationTestsAsync(config);
                        break;
                    case "performance":
                        result = await TestExecutionEngine.RunPerformanceTestsAsync(config);
                        break;
                    case "cicd":
                        result = await TestExecutionEngine.RunCICDTestsAsync(config);
                        break;
                    case "componentvalidation":
                        result = await TestExecutionEngine.RunComponentValidationTestsAsync(config);
                        break;
                    default:
                        throw new ArgumentException($"Unknown test category: {category}");
                }

                result.Category = category;
                result.Environment = environment;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTimeOffset.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }
        }

        /// <summary>
        /// Runs all test categories in sequence.
        /// </summary>
        public static async Task<TestExecutionResult> RunAllTestsAsync(string environment = "Development")
        {
            var result = new TestExecutionResult
            {
                Category = "All",
                Environment = environment,
                StartTime = DateTimeOffset.UtcNow
            };

            var categoryResults = new List<TestExecutionResult>();
            var successCount = 0;

            foreach (var category in TestCategories.All)
            {
                try
                {
                    var categoryResult = await RunTestCategoryAsync(category, environment);
                    categoryResults.Add(categoryResult);
                    
                    if (categoryResult.Success)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    var errorResult = new TestExecutionResult
                    {
                        Category = category,
                        Environment = environment,
                        Success = false,
                        ErrorMessage = ex.Message,
                        StartTime = DateTimeOffset.UtcNow,
                        EndTime = DateTimeOffset.UtcNow
                    };
                    categoryResults.Add(errorResult);
                }
            }

            result.Success = successCount == TestCategories.All.Length;
            result.CategoryResults = categoryResults;
            result.EndTime = DateTimeOffset.UtcNow;
            result.Duration = result.EndTime - result.StartTime;

            return result;
        }

        /// <summary>
        /// Exports test results to a file.
        /// </summary>
        public static async Task ExportTestReportAsync(TestExecutionResult result, string filePath)
        {
            await TestReportGenerator.ExportTestReportAsync(result, filePath);
        }
    }
}
