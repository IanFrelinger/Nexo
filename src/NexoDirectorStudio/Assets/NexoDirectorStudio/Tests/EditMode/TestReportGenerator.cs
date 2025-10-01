using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Generates test reports and handles report export.
    /// </summary>
    public static class TestReportGenerator
    {
        /// <summary>
        /// Generates a comprehensive test report.
        /// </summary>
        public static string GenerateTestReport(TestExecutionResult result)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== NEXO DIRECTOR TEST EXECUTION REPORT ===");
            report.AppendLine($"Category: {result.Category}");
            report.AppendLine($"Environment: {result.Environment}");
            report.AppendLine($"Start Time: {result.StartTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"End Time: {result.EndTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Duration: {result.Duration.TotalSeconds:F2}s");
            report.AppendLine($"Success: {(result.Success ? "✅ PASSED" : "❌ FAILED")}");
            report.AppendLine($"Tests Run: {result.TestsRun}");
            report.AppendLine($"Tests Passed: {result.TestsPassed}");
            report.AppendLine($"Tests Failed: {result.TestsFailed}");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                report.AppendLine($"Error: {result.ErrorMessage}");
            }

            if (result.PerformanceMetrics != null && result.PerformanceMetrics.Count > 0)
            {
                report.AppendLine("\n=== PERFORMANCE METRICS ===");
                foreach (var metric in result.PerformanceMetrics)
                {
                    report.AppendLine($"{metric.Key}: {metric.Value}");
                }
            }

            if (result.CategoryResults != null && result.CategoryResults.Count > 0)
            {
                report.AppendLine("\n=== CATEGORY RESULTS ===");
                foreach (var categoryResult in result.CategoryResults)
                {
                    report.AppendLine($"{categoryResult.Category}: {(categoryResult.Success ? "✅" : "❌")} " +
                        $"({categoryResult.TestsPassed}/{categoryResult.TestsRun})");
                }
            }

            return report.ToString();
        }

        /// <summary>
        /// Exports test results to a file.
        /// </summary>
        public static async Task ExportTestReportAsync(TestExecutionResult result, string filePath)
        {
            try
            {
                var report = GenerateTestReport(result);
                await System.IO.File.WriteAllTextAsync(filePath, report);
                Debug.Log($"📄 Test report exported to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export test report: {ex.Message}");
            }
        }
    }
}
