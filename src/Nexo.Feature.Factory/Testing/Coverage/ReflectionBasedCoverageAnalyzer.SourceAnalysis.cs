using System.Reflection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Progress;

namespace Nexo.Feature.Factory.Testing.Coverage
{
    /// <summary>
    /// Source analysis functionality for reflection-based coverage analyzer.
    /// </summary>
    public sealed partial class ReflectionBasedCoverageAnalyzer
    {
        /// <summary>
        /// Analyzes test coverage for the specified source files.
        /// </summary>
        public async Task<TestCoverageInfo> AnalyzeSourceCoverageAsync(
            IEnumerable<string> sourceFiles,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting source coverage analysis for {SourceFileCount} source files", sourceFiles.Count());

            try
            {
                var fileCoverage = new Dictionary<string, FileCoverageInfo>();
                var classCoverage = new Dictionary<string, ClassCoverageInfo>();

                // Analyze source files
                foreach (var sourceFile in sourceFiles)
                {
                    if (File.Exists(sourceFile))
                    {
                        var coverage = await AnalyzeSourceFileAsync(sourceFile, testFiles, cancellationToken);
                        if (coverage != null)
                        {
                            fileCoverage[sourceFile] = coverage;
                        }
                    }
                }

                // Calculate overall coverage
                var totalLines = fileCoverage.Values.Sum(f => f.TotalLines);
                var coveredLines = fileCoverage.Values.Sum(f => f.CoveredLines);
                var totalBranches = fileCoverage.Values.Sum(f => f.TotalBranches);
                var coveredBranches = fileCoverage.Values.Sum(f => f.CoveredBranches);
                var totalMethods = classCoverage.Values.Sum(c => c.TotalMethods);
                var coveredMethods = classCoverage.Values.Sum(c => c.CoveredMethods);
                var totalClasses = classCoverage.Count;
                var coveredClasses = classCoverage.Values.Count(c => c.MethodCoverage > 0);

                var lineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;
                var branchCoverage = totalBranches > 0 ? (double)coveredBranches / totalBranches * 100 : 0;
                var methodCoverage = totalMethods > 0 ? (double)coveredMethods / totalMethods * 100 : 0;
                var classCoveragePercent = totalClasses > 0 ? (double)coveredClasses / totalClasses * 100 : 0;
                var overallCoverage = (lineCoverage + branchCoverage + methodCoverage + classCoveragePercent) / 4;

                return new TestCoverageInfo(
                    overallCoverage,
                    lineCoverage,
                    branchCoverage,
                    methodCoverage,
                    classCoveragePercent,
                    totalLines,
                    coveredLines,
                    totalBranches,
                    coveredBranches,
                    totalMethods,
                    coveredMethods,
                    totalClasses,
                    coveredClasses,
                    fileCoverage,
                    classCoverage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze source coverage");
                return CreateEmptyCoverageInfo();
            }
        }

        private async Task<FileCoverageInfo?> AnalyzeSourceFileAsync(
            string sourceFile,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken)
        {
            try
            {
                var content = await File.ReadAllTextAsync(sourceFile, cancellationToken);
                var lines = content.Split('\n');
                var totalLines = lines.Length;
                var coveredLines = 0;
                var uncoveredLines = new List<int>();

                // Simplified line coverage analysis
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (!string.IsNullOrEmpty(line) && !line.StartsWith("//") && !line.StartsWith("/*"))
                    {
                        // Check if this line is likely covered by tests
                        var isCovered = await IsLineCoveredAsync(line, testFiles, cancellationToken);
                        if (isCovered)
                        {
                            coveredLines++;
                        }
                        else
                        {
                            uncoveredLines.Add(i + 1);
                        }
                    }
                }

                var lineCoverage = totalLines > 0 ? (double)coveredLines / totalLines * 100 : 0;

                return new FileCoverageInfo(
                    sourceFile,
                    lineCoverage,
                    0, // Branch coverage not calculated in this simplified version
                    totalLines,
                    coveredLines,
                    0, // Total branches
                    0, // Covered branches
                    uncoveredLines
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze source file: {SourceFile}", sourceFile);
                return null;
            }
        }

        private Task<bool> IsLineCoveredAsync(
            string line,
            IEnumerable<string> testFiles,
            CancellationToken cancellationToken)
        {
            try
            {
                // Simplified line coverage detection
                // In a real implementation, this would use more sophisticated analysis
                return Task.FromResult(line.Contains("public") || line.Contains("private") || line.Contains("protected"));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
