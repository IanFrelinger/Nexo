using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Service for analyzing the impact of code changes on tests.
    /// </summary>
    public class TestImpactAnalyzer : ITestImpactAnalyzer
    {
        private readonly ILogger<TestImpactAnalyzer> _logger;
        private readonly Dictionary<string, List<string>> _sourceTestCache;
        private readonly Dictionary<string, TestDependencyGraph> _dependencyGraphCache;

        public TestImpactAnalyzer(ILogger<TestImpactAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sourceTestCache = new Dictionary<string, List<string>>();
            _dependencyGraphCache = new Dictionary<string, TestDependencyGraph>();
        }

        public async Task<TestImpactAnalysis> AnalyzeImpactAsync(List<string> changedFiles, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Analyzing test impact for {Count} changed files", changedFiles.Count);

            try
            {
                var projectRoot = Directory.GetCurrentDirectory();
                var allTests = await DiscoverTestFilesAsync(projectRoot, cancellationToken);
                var affectedTests = await GetAffectedTestsAsync(changedFiles, cancellationToken);
                var confidence = await GetAnalysisConfidenceAsync(changedFiles, cancellationToken);

                var sourceTestMappings = new List<SourceTestMapping>();
                foreach (var changedFile in changedFiles)
                {
                    var mapping = await MapSourceToTestsAsync(changedFile, cancellationToken);
                    sourceTestMappings.Add(new SourceTestMapping
                    {
                        SourceFile = changedFile,
                        TestFiles = mapping,
                        Confidence = await CalculateMappingConfidenceAsync(changedFile, mapping, cancellationToken),
                        MappingStrategy = DetermineMappingStrategy(changedFile)
                    });
                }

                stopwatch.Stop();

                var analysis = new TestImpactAnalysis
                {
                    AffectedTests = affectedTests,
                    AllTests = allTests,
                    Confidence = confidence,
                    SourceTestMappings = sourceTestMappings,
                    Metadata = new TestImpactMetadata
                    {
                        Strategy = "MultiStrategy",
                        Reasoning = GenerateReasoning(changedFiles, affectedTests, allTests),
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        SourceFilesAnalyzed = changedFiles.Count,
                        TestFilesDiscovered = allTests.Count,
                        Warnings = GenerateWarnings(changedFiles, affectedTests, allTests)
                    }
                };

                _logger.LogInformation("Test impact analysis completed: {AffectedTests} affected out of {TotalTests} total tests (Confidence: {Confidence:P})",
                    affectedTests.Count, allTests.Count, confidence);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing test impact");
                return new TestImpactAnalysis
                {
                    AffectedTests = new List<string>(),
                    AllTests = new List<string>(),
                    Confidence = 0.0,
                    Metadata = new TestImpactMetadata
                    {
                        Strategy = "Fallback",
                        Reasoning = "Analysis failed, running all tests",
                        Warnings = new List<string> { $"Analysis error: {ex.Message}" }
                    }
                };
            }
        }

        public async Task<List<string>> GetAffectedTestsAsync(List<string> changedFiles, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting affected tests for {Count} changed files", changedFiles.Count);

            var affectedTests = new HashSet<string>();

            foreach (var changedFile in changedFiles)
            {
                var relatedTests = await MapSourceToTestsAsync(changedFile, cancellationToken);
                foreach (var test in relatedTests)
                {
                    affectedTests.Add(test);
                }
            }

            return affectedTests.ToList();
        }

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

        public async Task<double> GetAnalysisConfidenceAsync(List<string> changedFiles, CancellationToken cancellationToken = default)
        {
            if (!changedFiles.Any())
                return 1.0; // No changes means no impact

            var confidenceScores = new List<double>();

            foreach (var changedFile in changedFiles)
            {
                var fileConfidence = await CalculateFileConfidenceAsync(changedFile, cancellationToken);
                confidenceScores.Add(fileConfidence);
            }

            return confidenceScores.Any() ? confidenceScores.Average() : 0.5;
        }

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

        public async Task<TestDependencyGraph> BuildDependencyGraphAsync(string projectRoot, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Building test dependency graph for: {ProjectRoot}", projectRoot);

            // Check cache first
            if (_dependencyGraphCache.TryGetValue(projectRoot, out var cachedGraph))
            {
                return cachedGraph;
            }

            var graph = new TestDependencyGraph();
            var testFiles = await DiscoverTestFilesAsync(projectRoot, cancellationToken);

            try
            {
                // Build nodes
                foreach (var testFile in testFiles)
                {
                    var node = await CreateTestNodeAsync(testFile, cancellationToken);
                    graph.Nodes.Add(node);
                }

                // Build edges (simplified for now)
                foreach (var node in graph.Nodes)
                {
                    var dependencies = await FindTestDependenciesAsync(node, graph.Nodes, cancellationToken);
                    foreach (var dependency in dependencies)
                    {
                        graph.Edges.Add(new TestEdge
                        {
                            FromFilePath = node.FilePath,
                            ToFilePath = dependency.FilePath,
                            DependencyType = TestDependencyType.Direct,
                            Strength = 0.8
                        });
                    }
                }

                // Cache the graph
                _dependencyGraphCache[projectRoot] = graph;

                _logger.LogDebug("Built dependency graph with {Nodes} nodes and {Edges} edges", 
                    graph.Nodes.Count, graph.Edges.Count);

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building test dependency graph");
                return new TestDependencyGraph();
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

        private async Task<double> CalculateFileConfidenceAsync(string sourceFile, CancellationToken cancellationToken)
        {
            var confidence = 0.5; // Base confidence

            // Increase confidence based on file type
            var extension = Path.GetExtension(sourceFile).ToLowerInvariant();
            switch (extension)
            {
                case ".cs":
                    confidence += 0.2;
                    break;
                case ".csproj":
                    confidence += 0.1;
                    break;
                case ".json":
                case ".xml":
                    confidence += 0.05;
                    break;
            }

            // Increase confidence if we found related tests
            var relatedTests = await MapSourceToTestsAsync(sourceFile, cancellationToken);
            if (relatedTests.Any())
            {
                confidence += 0.2;
            }

            return Math.Min(confidence, 1.0);
        }

        private Task<double> CalculateMappingConfidenceAsync(string sourceFile, List<string> testFiles, CancellationToken cancellationToken)
        {
            if (!testFiles.Any())
                return Task.FromResult(0.1); // Low confidence if no tests found

            var confidence = 0.5; // Base confidence

            // Increase confidence based on mapping strategy
            var strategy = DetermineMappingStrategy(sourceFile);
            switch (strategy)
            {
                case "NamingConvention":
                    confidence += 0.3;
                    break;
                case "ProjectStructure":
                    confidence += 0.2;
                    break;
                case "ContentAnalysis":
                    confidence += 0.4;
                    break;
            }

            return Task.FromResult(Math.Min(confidence, 1.0));
        }

        private string DetermineMappingStrategy(string sourceFile)
        {
            // This is a simplified version - in practice, you'd track which strategies were successful
            return "MultiStrategy";
        }

        private string GenerateReasoning(List<string> changedFiles, List<string> affectedTests, List<string> allTests)
        {
            var ratio = allTests.Count > 0 ? (double)affectedTests.Count / allTests.Count : 0.0;
            
            if (ratio < 0.1)
                return $"Smart selection: {affectedTests.Count} tests selected out of {allTests.Count} total ({(ratio * 100):F1}% reduction)";
            else if (ratio < 0.5)
                return $"Moderate selection: {affectedTests.Count} tests selected out of {allTests.Count} total ({(ratio * 100):F1}% of tests)";
            else
                return $"Broad selection: {affectedTests.Count} tests selected out of {allTests.Count} total ({(ratio * 100):F1}% of tests) - consider running all tests";
        }

        private List<string> GenerateWarnings(List<string> changedFiles, List<string> affectedTests, List<string> allTests)
        {
            var warnings = new List<string>();

            if (affectedTests.Count == 0 && changedFiles.Any())
            {
                warnings.Add("No tests found for changed files - consider running all tests");
            }

            if (affectedTests.Count > allTests.Count * 0.8)
            {
                warnings.Add("High percentage of tests selected - consider running all tests for efficiency");
            }

            return warnings;
        }

        private Task<TestNode> CreateTestNodeAsync(string testFile, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TestNode
            {
                FilePath = testFile,
                ProjectName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(testFile) ?? ""),
                TestFramework = DetermineTestFramework(testFile),
                EstimatedExecutionTimeMs = 1000, // Default estimate
                Priority = 1,
                Categories = new List<string>()
            });
        }

        private string DetermineTestFramework(string testFile)
        {
            try
            {
                var content = File.ReadAllText(testFile);
                if (content.Contains("xunit"))
                    return "xUnit";
                if (content.Contains("nunit"))
                    return "NUnit";
                if (content.Contains("mstest"))
                    return "MSTest";
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private Task<List<TestNode>> FindTestDependenciesAsync(TestNode node, List<TestNode> allNodes, CancellationToken cancellationToken)
        {
            // Simplified dependency detection - in practice, you'd analyze test content
            return Task.FromResult(new List<TestNode>());
        }
    }
}