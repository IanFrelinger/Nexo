using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Report generation functionality for ReflectionBasedCoverageAnalyzer.
    /// </summary>
    public sealed partial class ReflectionBasedCoverageAnalyzer
    {
        /// <summary>
        /// Generates a coverage report in the specified format.
        /// </summary>
        public async Task GenerateCoverageReportAsync(
            TestCoverageInfo coverage,
            string outputPath,
            CoverageReportFormat format,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating coverage report in {Format} format to {OutputPath}", format, outputPath);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

                switch (format)
                {
                    case CoverageReportFormat.Html:
                        await GenerateHtmlReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Json:
                        await GenerateJsonReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Xml:
                        await GenerateXmlReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Text:
                        await GenerateTextReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    case CoverageReportFormat.Markdown:
                        await GenerateMarkdownReportAsync(coverage, outputPath, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported report format: {format}", nameof(format));
                }

                _logger.LogInformation("Coverage report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate coverage report");
                throw;
            }
        }

        private async Task GenerateHtmlReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Test Coverage Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f0f0f0; padding: 20px; border-radius: 5px; }}
        .metric {{ margin: 10px 0; }}
        .progress-bar {{ width: 100%; background-color: #e0e0e0; border-radius: 5px; overflow: hidden; }}
        .progress-fill {{ height: 20px; background-color: #4CAF50; transition: width 0.3s; }}
        .low-coverage {{ background-color: #f44336; }}
        .medium-coverage {{ background-color: #ff9800; }}
        .high-coverage {{ background-color: #4CAF50; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Test Coverage Report</h1>
        <p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <h2>Overall Coverage: {coverage.OverallCoverage:F1}%</h2>
    
    <div class=""metric"">
        <h3>Line Coverage: {coverage.LineCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.LineCoverage}%""></div>
        </div>
        <p>{coverage.CoveredLines} of {coverage.TotalLines} lines covered</p>
    </div>
    
    <div class=""metric"">
        <h3>Branch Coverage: {coverage.BranchCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.BranchCoverage}%""></div>
        </div>
        <p>{coverage.CoveredBranches} of {coverage.TotalBranches} branches covered</p>
    </div>
    
    <div class=""metric"">
        <h3>Method Coverage: {coverage.MethodCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.MethodCoverage}%""></div>
        </div>
        <p>{coverage.CoveredMethods} of {coverage.TotalMethods} methods covered</p>
    </div>
    
    <div class=""metric"">
        <h3>Class Coverage: {coverage.ClassCoverage:F1}%</h3>
        <div class=""progress-bar"">
            <div class=""progress-fill"" style=""width: {coverage.ClassCoverage}%""></div>
        </div>
        <p>{coverage.CoveredClasses} of {coverage.TotalClasses} classes covered</p>
    </div>
</body>
</html>";

            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
        }

        private async Task GenerateJsonReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputPath, json, cancellationToken);
        }

        private async Task GenerateXmlReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<coverage>
    <overall>{coverage.OverallCoverage:F1}</overall>
    <line>{coverage.LineCoverage:F1}</line>
    <branch>{coverage.BranchCoverage:F1}</branch>
    <method>{coverage.MethodCoverage:F1}</method>
    <class>{coverage.ClassCoverage:F1}</class>
    <lines total=""{coverage.TotalLines}"" covered=""{coverage.CoveredLines}"" />
    <branches total=""{coverage.TotalBranches}"" covered=""{coverage.CoveredBranches}"" />
    <methods total=""{coverage.TotalMethods}"" covered=""{coverage.CoveredMethods}"" />
    <classes total=""{coverage.TotalClasses}"" covered=""{coverage.CoveredClasses}"" />
</coverage>";

            await File.WriteAllTextAsync(outputPath, xml, cancellationToken);
        }

        private async Task GenerateTextReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var text = $@"Test Coverage Report
==================
Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Overall Coverage: {coverage.OverallCoverage:F1}%
Line Coverage: {coverage.LineCoverage:F1}% ({coverage.CoveredLines}/{coverage.TotalLines})
Branch Coverage: {coverage.BranchCoverage:F1}% ({coverage.CoveredBranches}/{coverage.TotalBranches})
Method Coverage: {coverage.MethodCoverage:F1}% ({coverage.CoveredMethods}/{coverage.TotalMethods})
Class Coverage: {coverage.ClassCoverage:F1}% ({coverage.CoveredClasses}/{coverage.TotalClasses})
";

            await File.WriteAllTextAsync(outputPath, text, cancellationToken);
        }

        private async Task GenerateMarkdownReportAsync(TestCoverageInfo coverage, string outputPath, CancellationToken cancellationToken)
        {
            var markdown = $@"# Test Coverage Report

Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

## Overall Coverage: {coverage.OverallCoverage:F1}%

| Metric | Coverage | Details |
|--------|----------|---------|
| Line Coverage | {coverage.LineCoverage:F1}% | {coverage.CoveredLines}/{coverage.TotalLines} lines |
| Branch Coverage | {coverage.BranchCoverage:F1}% | {coverage.CoveredBranches}/{coverage.TotalBranches} branches |
| Method Coverage | {coverage.MethodCoverage:F1}% | {coverage.CoveredMethods}/{coverage.TotalMethods} methods |
| Class Coverage | {coverage.ClassCoverage:F1}% | {coverage.CoveredClasses}/{coverage.TotalClasses} classes |

## File Coverage

| File | Line Coverage | Branch Coverage |
|------|---------------|-----------------|
";

            foreach (var file in coverage.FileCoverage.Take(10))
            {
                markdown += $"| {Path.GetFileName(file.Key)} | {file.Value.LineCoverage:F1}% | {file.Value.BranchCoverage:F1}% |\n";
            }

            await File.WriteAllTextAsync(outputPath, markdown, cancellationToken);
        }
    }
}
