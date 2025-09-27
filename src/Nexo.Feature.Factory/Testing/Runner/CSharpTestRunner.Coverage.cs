using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Coverage;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Test coverage analysis functionality
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        private async Task AnalyzeAndReportCoverageAsync(TestConfiguration configuration, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Analyzing test coverage...");

                // Get source assemblies from the current domain
                var sourceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && 
                               !a.FullName?.StartsWith("System.") == true &&
                               !a.FullName?.StartsWith("Microsoft.") == true &&
                               !a.FullName?.StartsWith("Nexo.Feature.Factory.Testing") == true)
                    .Select(a => a.Location)
                    .Where(File.Exists)
                    .ToList();

                // Get test assemblies
                var testAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && a.FullName?.Contains("Test") == true)
                    .Select(a => a.Location)
                    .Where(File.Exists)
                    .ToList();

                if (sourceAssemblies.Any() && testAssemblies.Any())
                {
                    var coverage = await _coverageAnalyzer.AnalyzeCoverageAsync(
                        sourceAssemblies, testAssemblies, cancellationToken);

                    _progressReporter.ReportCoverage(coverage);

                    // Generate coverage reports if output directory is specified
                    if (!string.IsNullOrEmpty(configuration.OutputDirectory))
                    {
                        var outputDir = configuration.OutputDirectory;
                        Directory.CreateDirectory(outputDir);

                        // Generate multiple report formats
                        await _coverageAnalyzer.GenerateCoverageReportAsync(
                            coverage, Path.Combine(outputDir, "coverage.html"), 
                            CoverageReportFormat.Html, cancellationToken);

                        await _coverageAnalyzer.GenerateCoverageReportAsync(
                            coverage, Path.Combine(outputDir, "coverage.json"), 
                            CoverageReportFormat.Json, cancellationToken);

                        await _coverageAnalyzer.GenerateCoverageReportAsync(
                            coverage, Path.Combine(outputDir, "coverage.md"), 
                            CoverageReportFormat.Markdown, cancellationToken);

                        _logger.LogInformation("Coverage reports generated in: {OutputDirectory}", outputDir);
                    }
                }
                else
                {
                    _logger.LogWarning("No source or test assemblies found for coverage analysis");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze test coverage");
                _progressReporter.ReportWarning("Coverage Analysis", $"Failed to analyze coverage: {ex.Message}");
            }
        }
    }
}
