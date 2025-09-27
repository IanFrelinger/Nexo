using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Core test impact analysis functionality
    /// </summary>
    public partial class TestImpactAnalyzer
    {
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
    }
}
