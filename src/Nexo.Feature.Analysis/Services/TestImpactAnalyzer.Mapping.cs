using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Source to test mapping functionality
    /// </summary>
    public partial class TestImpactAnalyzer
    {
        public async Task<List<string>> MapSourceToTestsAsync(string sourceFile, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Mapping source file to tests: {SourceFile}", sourceFile);

            // Check cache first
            if (_sourceTestCache.TryGetValue(sourceFile, out var cachedTests))
            {
                return cachedTests;
            }

            var relatedTests = new List<string>();

            try
            {
                // Strategy 1: Direct naming convention mapping
                var namingTests = await MapByNamingConventionAsync(sourceFile, cancellationToken);
                relatedTests.AddRange(namingTests);

                // Strategy 2: Project structure mapping
                var structureTests = await MapByProjectStructureAsync(sourceFile, cancellationToken);
                relatedTests.AddRange(structureTests);

                // Strategy 3: Content-based mapping (if file is small enough)
                if (await ShouldAnalyzeContentAsync(sourceFile, cancellationToken))
                {
                    var contentTests = await MapByContentAnalysisAsync(sourceFile, cancellationToken);
                    relatedTests.AddRange(contentTests);
                }

                // Remove duplicates and cache result
                var uniqueTests = relatedTests.Distinct().ToList();
                _sourceTestCache[sourceFile] = uniqueTests;

                _logger.LogDebug("Mapped {SourceFile} to {Count} test files", sourceFile, uniqueTests.Count);
                return uniqueTests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mapping source file {SourceFile} to tests", sourceFile);
                return new List<string>();
            }
        }

        private Task<List<string>> MapByNamingConventionAsync(string sourceFile, CancellationToken cancellationToken)
        {
            var relatedTests = new List<string>();
            var fileName = Path.GetFileNameWithoutExtension(sourceFile);
            var projectRoot = Directory.GetCurrentDirectory();

            // Common naming patterns
            var testPatterns = new[]
            {
                $"{fileName}Tests.cs",
                $"{fileName}Test.cs",
                $"{fileName}.Tests.cs",
                $"{fileName}.Test.cs"
            };

            foreach (var pattern in testPatterns)
            {
                var testFiles = Directory.GetFiles(projectRoot, $"**/{pattern}", SearchOption.AllDirectories);
                relatedTests.AddRange(testFiles);
            }

            return Task.FromResult(relatedTests);
        }

        private Task<List<string>> MapByProjectStructureAsync(string sourceFile, CancellationToken cancellationToken)
        {
            var relatedTests = new List<string>();
            var sourceDir = Path.GetDirectoryName(sourceFile);
            var projectRoot = Directory.GetCurrentDirectory();

            if (string.IsNullOrEmpty(sourceDir))
                return Task.FromResult(relatedTests);

            // Look for test directories at the same level
            var sourceDirInfo = new DirectoryInfo(sourceDir);
            var parentDir = sourceDirInfo.Parent;

            if (parentDir != null)
            {
                // Check for test directories
                var testDirs = parentDir.GetDirectories()
                    .Where(d => d.Name.IndexOf("Test", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();

                foreach (var testDir in testDirs)
                {
                    var testFiles = Directory.GetFiles(testDir.FullName, "*.cs", SearchOption.AllDirectories);
                    relatedTests.AddRange(testFiles);
                }
            }

            return Task.FromResult(relatedTests);
        }

        private async Task<List<string>> MapByContentAnalysisAsync(string sourceFile, CancellationToken cancellationToken)
        {
            var relatedTests = new List<string>();

            try
            {
                if (!File.Exists(sourceFile))
                    return relatedTests;

                var content = File.ReadAllText(sourceFile);
                var className = ExtractClassName(content);

                if (string.IsNullOrEmpty(className))
                    return relatedTests;

                // Look for test files that reference this class
                var projectRoot = Directory.GetCurrentDirectory();
                var testFiles = await DiscoverTestFilesAsync(projectRoot, cancellationToken);

                foreach (var testFile in testFiles)
                {
                    if (await FileContainsReferenceAsync(testFile, className, cancellationToken))
                    {
                        relatedTests.Add(testFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error in content analysis for {SourceFile}", sourceFile);
            }

            return relatedTests;
        }

        private string ExtractClassName(string content)
        {
            // Simple regex to extract class name
            var match = Regex.Match(content, @"class\s+(\w+)");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private Task<bool> FileContainsReferenceAsync(string filePath, string className, CancellationToken cancellationToken)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                return Task.FromResult(content.Contains(className));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
