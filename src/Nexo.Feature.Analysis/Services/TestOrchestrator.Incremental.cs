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
    public partial class TestOrchestrator
    {
        public async Task<IncrementalTestingResult> ExecuteIncrementalTestsAsync(IncrementalTestingOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Executing incremental tests with base reference: {BaseReference}", options.BaseReference);

            try
            {
                // Get smart test selection for changes
                var smartSelectionOptions = new SmartTestSelectionOptions
                {
                    MinimumConfidence = options.ConfidenceThreshold,
                    IncludeIndirectDependencies = options.IncludeDependentTests,
                    FallbackToAllTests = options.FallbackToFullSuite
                };

                var smartSelectionResult = await _smartTestSelector.SelectTestsAsync(smartSelectionOptions, cancellationToken);

                if (!smartSelectionResult.UsedSmartSelection || smartSelectionResult.Confidence < options.ConfidenceThreshold)
                {
                    if (options.FallbackToFullSuite)
                    {
                        _logger.LogInformation("Confidence too low ({Confidence}), falling back to full test suite", smartSelectionResult.Confidence);
                        var allTests = await GetAllTestFilesAsync(cancellationToken);
                        var fullSuiteResults = await ExecuteTestsInParallelAsync(allTests, new ParallelExecutionOptions
                        {
                            MaxParallelism = Environment.ProcessorCount,
                            TestTimeout = TimeSpan.FromMinutes(5),
                            ContinueOnFailure = true
                        }, cancellationToken);

                        return new IncrementalTestingResult
                        {
                            IsSuccess = fullSuiteResults.IsSuccess,
                            TestsExecuted = fullSuiteResults.TestsExecuted,
                            TotalTestsInSuite = allTests.Count,
                            TimeSaved = TimeSpan.Zero,
                            Confidence = 0.0,
                            Results = fullSuiteResults.Results,
                            UsedFallback = true
                        };
                    }
                    else
                    {
                        return new IncrementalTestingResult
                        {
                            IsSuccess = false,
                            ErrorMessage = $"Confidence too low: {smartSelectionResult.Confidence}"
                        };
                    }
                }

                // Execute selected tests
                var selectedTests = smartSelectionResult.SelectedTests;
                var parallelOptions = new ParallelExecutionOptions
                {
                    MaxParallelism = Environment.ProcessorCount,
                    TestTimeout = TimeSpan.FromMinutes(5),
                    ContinueOnFailure = true
                };

                var parallelResult = await ExecuteTestsInParallelAsync(selectedTests, parallelOptions, cancellationToken);

                // Calculate time savings
                var totalTime = stopwatch.Elapsed;
                var estimatedFullSuiteTime = TimeSpan.FromMinutes(selectedTests.Count * 2); // Rough estimate
                var timeSaved = estimatedFullSuiteTime - totalTime;

                var result = new IncrementalTestingResult
                {
                    IsSuccess = parallelResult.IsSuccess,
                    TestsExecuted = parallelResult.TestsExecuted,
                    TotalTestsInSuite = smartSelectionResult.AllTests.Count,
                    TimeSaved = timeSaved,
                    Confidence = smartSelectionResult.Confidence,
                    Results = parallelResult.Results,
                    UsedFallback = false
                };

                _logger.LogInformation("Incremental testing completed: {Executed}/{Total} tests in {Duration}ms (Time saved: {TimeSaved})",
                    parallelResult.TestsExecuted, smartSelectionResult.AllTests.Count, totalTime.TotalMilliseconds, timeSaved);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in incremental testing");
                return new IncrementalTestingResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
