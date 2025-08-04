using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Service for intelligent test selection based on code changes and impact analysis.
    /// </summary>
    public class SmartTestSelector : ISmartTestSelector
    {
        private readonly ILogger<SmartTestSelector> _logger;
        private readonly IGitChangeDetector _gitChangeDetector;
        private readonly ITestImpactAnalyzer _testImpactAnalyzer;

        public SmartTestSelector(
            ILogger<SmartTestSelector> logger,
            IGitChangeDetector gitChangeDetector,
            ITestImpactAnalyzer testImpactAnalyzer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gitChangeDetector = gitChangeDetector ?? throw new ArgumentNullException(nameof(gitChangeDetector));
            _testImpactAnalyzer = testImpactAnalyzer ?? throw new ArgumentNullException(nameof(testImpactAnalyzer));
        }

        public async Task<SmartTestSelectionResult> SelectTestsAsync(SmartTestSelectionOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting smart test selection with confidence threshold: {Confidence}", options.MinimumConfidence);

            try
            {
                // Validate options
                var validation = ValidateOptions(options);
                if (!validation.IsValid)
                {
                    _logger.LogError("Invalid smart test selection options: {Errors}", string.Join(", ", validation.Errors));
                    return CreateFallbackResult("Invalid options", validation.Errors);
                }

                // Get uncommitted changes by default
                var changedFiles = await _gitChangeDetector.GetUncommittedChangesAsync(cancellationToken);
                
                if (!changedFiles.Any())
                {
                    _logger.LogInformation("No uncommitted changes detected, running all tests");
                    return await CreateAllTestsResultAsync(cancellationToken);
                }

                return await SelectTestsForChangedFilesAsync(changedFiles, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in smart test selection");
                return CreateFallbackResult("Selection failed", new List<string> { ex.Message });
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Smart test selection completed in {Duration}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task<SmartTestSelectionResult> SelectTestsForChangedFilesAsync(List<string> changedFiles, SmartTestSelectionOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Selecting tests for {Count} changed files", changedFiles.Count);

            try
            {
                // Filter changed files based on options
                var filteredFiles = await FilterChangedFilesAsync(changedFiles, options, cancellationToken);
                
                if (!filteredFiles.Any())
                {
                    _logger.LogInformation("No relevant changes detected after filtering");
                    return await CreateAllTestsResultAsync(cancellationToken);
                }

                // Perform impact analysis
                var gitAnalysisTime = Stopwatch.StartNew();
                var impactAnalysis = await _testImpactAnalyzer.AnalyzeImpactAsync(filteredFiles, cancellationToken);
                gitAnalysisTime.Stop();

                // Evaluate confidence and make selection decision
                var selectionDecision = EvaluateSelectionDecision(impactAnalysis, options);
                
                var result = new SmartTestSelectionResult
                {
                    ChangedFiles = filteredFiles,
                    ImpactAnalysis = impactAnalysis,
                    Confidence = impactAnalysis.Confidence,
                    UsedSmartSelection = selectionDecision.UseSmartSelection,
                    SelectionReason = selectionDecision.Reason,
                    Warnings = impactAnalysis.Metadata.Warnings,
                    Metrics = new SmartTestSelectionMetrics
                    {
                        TotalTimeMs = stopwatch.ElapsedMilliseconds,
                        GitAnalysisTimeMs = gitAnalysisTime.ElapsedMilliseconds,
                        ImpactAnalysisTimeMs = impactAnalysis.Metadata.DurationMs,
                        FilesAnalyzed = filteredFiles.Count,
                        TestsDiscovered = impactAnalysis.AllTests.Count,
                        TestsSelected = selectionDecision.UseSmartSelection ? impactAnalysis.AffectedTests.Count : impactAnalysis.AllTests.Count
                    }
                };

                if (selectionDecision.UseSmartSelection)
                {
                    result.SelectedTests = impactAnalysis.AffectedTests;
                    result.AllTests = impactAnalysis.AllTests;
                    
                    if (options.IncludeIndirectDependencies)
                    {
                        var indirectTests = await GetIndirectDependenciesAsync(impactAnalysis, cancellationToken);
                        result.SelectedTests.AddRange(indirectTests);
                        result.SelectedTests = result.SelectedTests.Distinct().ToList();
                    }
                }
                else
                {
                    result.SelectedTests = impactAnalysis.AllTests;
                    result.AllTests = impactAnalysis.AllTests;
                }

                stopwatch.Stop();
                result.Metrics.TotalTimeMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Smart test selection completed: {Selected} tests selected out of {Total} total (Confidence: {Confidence:P})",
                    result.SelectedTests.Count, result.AllTests.Count, result.Confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting tests for changed files");
                return CreateFallbackResult("Analysis failed", new List<string> { ex.Message });
            }
        }

        public async Task<SmartTestSelectionResult> SelectTestsForGitChangesAsync(string sinceReference, SmartTestSelectionOptions options, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Selecting tests for Git changes since: {SinceReference}", sinceReference);

            try
            {
                var changedFiles = await _gitChangeDetector.GetChangedFilesAsync(sinceReference, cancellationToken);
                return await SelectTestsForChangedFilesAsync(changedFiles, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting tests for Git changes since {SinceReference}", sinceReference);
                return CreateFallbackResult("Git analysis failed", new List<string> { ex.Message });
            }
        }

        public string GetSelectionSummary(SmartTestSelectionResult result)
        {
            var summary = new System.Text.StringBuilder();
            
            summary.AppendLine($"🧠 Smart Test Selection Summary");
            summary.AppendLine($"=================================");
            summary.AppendLine($"📊 Selection Method: {(result.UsedSmartSelection ? "Smart Analysis" : "All Tests")}");
            summary.AppendLine($"🎯 Tests Selected: {result.SelectedTests.Count:N0} out of {result.AllTests.Count:N0} total");
            summary.AppendLine($"📈 Confidence Level: {result.Confidence:P1}");
            summary.AppendLine($"⚡ Performance: {result.Metrics.TotalTimeMs}ms total");
            summary.AppendLine($"📁 Files Analyzed: {result.Metrics.FilesAnalyzed:N0}");
            summary.AppendLine($"🔍 Selection Reason: {result.SelectionReason}");
            
            if (result.Warnings.Any())
            {
                summary.AppendLine($"⚠️  Warnings:");
                foreach (var warning in result.Warnings)
                {
                    summary.AppendLine($"   • {warning}");
                }
            }

            if (result.UsedSmartSelection && result.ImpactAnalysis != null)
            {
                summary.AppendLine($"📋 Impact Analysis:");
                summary.AppendLine($"   • Strategy: {result.ImpactAnalysis.Metadata.Strategy}");
                summary.AppendLine($"   • Duration: {result.ImpactAnalysis.Metadata.DurationMs}ms");
                summary.AppendLine($"   • Selection Ratio: {result.ImpactAnalysis.SelectionRatio:P1}");
            }

            return summary.ToString();
        }

        public SmartTestSelectionValidation ValidateOptions(SmartTestSelectionOptions options)
        {
            var validation = new SmartTestSelectionValidation { IsValid = true };

            if (options.MinimumConfidence < 0.0 || options.MinimumConfidence > 1.0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MinimumConfidence must be between 0.0 and 1.0");
            }

            if (options.MaximumSelectionRatio < 0.0 || options.MaximumSelectionRatio > 1.0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaximumSelectionRatio must be between 0.0 and 1.0");
            }

            if (options.MinimumConfidence > options.MaximumSelectionRatio)
            {
                validation.Warnings.Add("MinimumConfidence is higher than MaximumSelectionRatio - this may result in no tests being selected");
            }

            if (options.ForceRefresh && options.UseCache)
            {
                validation.Warnings.Add("ForceRefresh and UseCache are both enabled - ForceRefresh will take precedence");
            }

            return validation;
        }

        private Task<List<string>> FilterChangedFilesAsync(List<string> changedFiles, SmartTestSelectionOptions options, CancellationToken cancellationToken)
        {
            var filteredFiles = new List<string>();

            foreach (var file in changedFiles)
            {
                var shouldInclude = true;

                // Check if it's a configuration file
                if (IsConfigFile(file) && !options.IncludeConfigFileTests)
                {
                    shouldInclude = false;
                }

                // Check if it's an infrastructure file
                if (IsInfrastructureFile(file) && !options.IncludeInfrastructureTests)
                {
                    shouldInclude = false;
                }

                if (shouldInclude)
                {
                    filteredFiles.Add(file);
                }
            }

            _logger.LogDebug("Filtered {OriginalCount} changed files to {FilteredCount} relevant files", 
                changedFiles.Count, filteredFiles.Count);

            return Task.FromResult(filteredFiles);
        }

        private bool IsConfigFile(string filePath)
        {
            var fileName = System.IO.Path.GetFileName(filePath).ToLowerInvariant();
            var configExtensions = new[] { ".json", ".xml", ".config", ".yml", ".yaml" };
            var configNames = new[] { "appsettings", "web.config", "packages.config", "nuget.config" };

            return configExtensions.Any(ext => fileName.EndsWith(ext)) ||
                   configNames.Any(name => fileName.Contains(name));
        }

        private bool IsInfrastructureFile(string filePath)
        {
            var fileName = System.IO.Path.GetFileName(filePath).ToLowerInvariant();
            var infrastructureNames = new[] { "dockerfile", "docker-compose", "dockerfile.unity", "global.json" };

            return infrastructureNames.Any(name => fileName.Contains(name));
        }

        private (bool UseSmartSelection, string Reason) EvaluateSelectionDecision(TestImpactAnalysis impactAnalysis, SmartTestSelectionOptions options)
        {
            // Check confidence threshold
            if (impactAnalysis.Confidence < options.MinimumConfidence)
            {
                return (false, $"Confidence {impactAnalysis.Confidence:P1} below threshold {options.MinimumConfidence:P1}");
            }

            // Check selection ratio
            if (impactAnalysis.SelectionRatio > options.MaximumSelectionRatio)
            {
                return (false, $"Selection ratio {impactAnalysis.SelectionRatio:P1} above maximum {options.MaximumSelectionRatio:P1}");
            }

            // Check if we have any affected tests
            if (!impactAnalysis.AffectedTests.Any())
            {
                return (false, "No affected tests found");
            }

            // Check if fallback is disabled and we have warnings
            if (!options.FallbackToAllTests && impactAnalysis.Metadata.Warnings.Any())
            {
                return (false, "Fallback disabled and warnings present");
            }

            return (true, $"Smart selection with {impactAnalysis.Confidence:P1} confidence and {impactAnalysis.SelectionRatio:P1} selection ratio");
        }

        private async Task<List<string>> GetIndirectDependenciesAsync(TestImpactAnalysis impactAnalysis, CancellationToken cancellationToken)
        {
            var indirectTests = new List<string>();

            try
            {
                // Build dependency graph
                var projectRoot = System.IO.Directory.GetCurrentDirectory();
                var dependencyGraph = await _testImpactAnalyzer.BuildDependencyGraphAsync(projectRoot, cancellationToken);

                // Find tests that depend on the affected tests
                foreach (var affectedTest in impactAnalysis.AffectedTests)
                {
                    var dependents = dependencyGraph.GetDependents(affectedTest);
                    indirectTests.AddRange(dependents);
                }

                indirectTests = indirectTests.Distinct().ToList();
                _logger.LogDebug("Found {Count} indirect dependencies", indirectTests.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding indirect dependencies");
            }

            return indirectTests;
        }

        private async Task<SmartTestSelectionResult> CreateAllTestsResultAsync(CancellationToken cancellationToken)
        {
            var projectRoot = System.IO.Directory.GetCurrentDirectory();
            var allTests = await _testImpactAnalyzer.DiscoverTestFilesAsync(projectRoot, cancellationToken);

            return new SmartTestSelectionResult
            {
                SelectedTests = allTests,
                AllTests = allTests,
                Confidence = 1.0,
                UsedSmartSelection = false,
                SelectionReason = "Running all tests (no changes or fallback)",
                ChangedFiles = new List<string>(),
                Warnings = new List<string>(),
                Metrics = new SmartTestSelectionMetrics
                {
                    TestsDiscovered = allTests.Count,
                    TestsSelected = allTests.Count
                }
            };
        }

        private SmartTestSelectionResult CreateFallbackResult(string reason, List<string> warnings)
        {
            return new SmartTestSelectionResult
            {
                SelectedTests = new List<string>(),
                AllTests = new List<string>(),
                Confidence = 0.0,
                UsedSmartSelection = false,
                SelectionReason = reason,
                ChangedFiles = new List<string>(),
                Warnings = warnings,
                Metrics = new SmartTestSelectionMetrics()
            };
        }
    }
}