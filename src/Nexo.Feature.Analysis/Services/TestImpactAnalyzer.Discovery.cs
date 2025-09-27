using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Test file discovery functionality
    /// </summary>
    public partial class TestImpactAnalyzer
    {
        public Task<List<string>> DiscoverTestFilesAsync(string projectRoot, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Discovering test files in: {ProjectRoot}", projectRoot);

            var testFiles = new List<string>();
            var testPatterns = new[]
            {
                "**/*Tests.cs",
                "**/*Test.cs",
                "**/*.Tests.csproj",
                "**/*.Test.csproj",
                "**/test/**/*.cs",
                "**/tests/**/*.cs"
            };

            try
            {
                foreach (var pattern in testPatterns)
                {
                    var files = Directory.GetFiles(projectRoot, pattern, SearchOption.AllDirectories);
                    testFiles.AddRange(files);
                }

                // Filter out non-test files and normalize paths
                var filteredTests = testFiles
                    .Where(f => IsTestFile(f))
                    .Select(f => Path.GetFullPath(f))
                    .Distinct()
                    .ToList();

                _logger.LogDebug("Discovered {Count} test files", filteredTests.Count);
                return Task.FromResult(filteredTests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering test files in {ProjectRoot}", projectRoot);
                return Task.FromResult(new List<string>());
            }
        }

        private bool IsTestFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(filePath);

            if (extension != ".cs" && extension != ".csproj")
                return false;

            // Check for test indicators in filename
            var testIndicators = new[] { "Test", "Tests", "Spec", "Specs" };
            return testIndicators.Any(indicator => fileName.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private Task<bool> ShouldAnalyzeContentAsync(string sourceFile, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = new FileInfo(sourceFile);
                return Task.FromResult(fileInfo.Exists && fileInfo.Length < 1024 * 1024); // 1MB limit
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
