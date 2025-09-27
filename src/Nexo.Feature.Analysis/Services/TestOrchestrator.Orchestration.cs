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
    public partial class TestOrchestrator
    {
        public async Task<TestOrchestrationResult> ExecuteTestsAsync(TestOrchestrationOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting intelligent test orchestration with parallel execution: {Parallel}, dependency ordering: {Dependency}, incremental: {Incremental}",
                options.UseParallelExecution, options.UseDependencyOrdering, options.UseIncrementalTesting);

            try
            {
                // Validate options
                var validation = ValidateOptions(options);
                if (!validation.IsValid)
                {
                    _logger.LogError("Invalid test orchestration options: {Errors}", string.Join(", ", validation.Errors));
                    return CreateFailedResult("Invalid options", validation.Errors);
                }

                // Get resource utilization
                var resourceUtilization = await GetResourceUtilizationAsync(cancellationToken);
                
                // Adjust options based on resource constraints
                AdjustOptionsForResources(options, resourceUtilization);

                // Determine which tests to execute
                List<string> testFiles;
                if (options.UseIncrementalTesting)
                {
                    var incrementalResult = await ExecuteIncrementalTestsAsync(new IncrementalTestingOptions
                    {
                        BaseReference = "HEAD~1",
                        UseCachedResults = true,
                        RunAffectedTestsOnly = true,
                        IncludeDependentTests = true,
                        ConfidenceThreshold = 0.8,
                        FallbackToFullSuite = true
                    }, cancellationToken);

                    if (!incrementalResult.IsSuccess)
                    {
                        _logger.LogWarning("Incremental testing failed, falling back to full test suite");
                        testFiles = await GetAllTestFilesAsync(cancellationToken);
                    }
                    else
                    {
                        testFiles = incrementalResult.Results.Select(r => r.TestFile).ToList();
                    }
                }
                else
                {
                    testFiles = await GetAllTestFilesAsync(cancellationToken);
                }

                // Filter tests based on categories
                testFiles = FilterTestsByCategories(testFiles, options);

                if (!testFiles.Any())
                {
                    _logger.LogWarning("No tests found to execute");
                    return CreateEmptyResult();
                }

                // Create execution plan
                TestExecutionPlan executionPlan;
                if (options.UseDependencyOrdering)
                {
                    executionPlan = await CreateDependencyOrderedPlanAsync(testFiles, new DependencyOrderingOptions
                    {
                        AutoDetectDependencies = true,
                        RespectExplicitDependencies = true,
                        GroupIndependentTests = true,
                        MaxGroupSize = options.MaxParallelism,
                        ValidateCycles = true
                    }, cancellationToken);
                }
                else
                {
                    executionPlan = CreateSimplePlan(testFiles, options);
                }

                if (!executionPlan.IsValid)
                {
                    _logger.LogError("Invalid execution plan: {Errors}", string.Join(", ", executionPlan.ValidationErrors));
                    return CreateFailedResult("Invalid execution plan", executionPlan.ValidationErrors);
                }

                // Execute tests according to plan
                var testResults = new List<TestExecutionResult>();
                var parallelMetrics = new ParallelExecutionMetrics
                {
                    SequentialTime = TimeSpan.FromMinutes(executionPlan.EstimatedExecutionTime.TotalMinutes * executionPlan.Phases.Count)
                };

                foreach (var phase in executionPlan.Phases)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Test execution cancelled");
                        break;
                    }

                    var phaseResults = await ExecutePhaseAsync(phase, options, cancellationToken);
                    testResults.AddRange(phaseResults);

                    // Check for early termination
                    if (options.StopOnFirstFailure && phaseResults.Any(r => !r.IsSuccess))
                    {
                        _logger.LogWarning("Stopping execution due to test failure");
                        break;
                    }
                }

                // Calculate metrics
                var totalExecutionTime = stopwatch.Elapsed;
                var passedTests = testResults.Count(r => r.IsSuccess);
                var failedTests = testResults.Count(r => !r.IsSuccess);
                var skippedTests = testResults.Count(r => r.ExitCode == -1); // Assuming -1 means skipped

                parallelMetrics.TotalTime = totalExecutionTime;
                parallelMetrics.MaxParallelism = options.MaxParallelism;
                parallelMetrics.AverageParallelism = CalculateAverageParallelism(testResults);
                parallelMetrics.SpeedupFactor = parallelMetrics.SequentialTime.TotalMilliseconds / totalExecutionTime.TotalMilliseconds;
                parallelMetrics.Efficiency = parallelMetrics.SpeedupFactor / options.MaxParallelism;

                var result = new TestOrchestrationResult
                {
                    IsSuccess = failedTests == 0,
                    TotalTests = testResults.Count,
                    PassedTests = passedTests,
                    FailedTests = failedTests,
                    SkippedTests = skippedTests,
                    TotalExecutionTime = totalExecutionTime,
                    ParallelMetrics = parallelMetrics,
                    ResourceUtilization = resourceUtilization,
                    TestResults = testResults,
                    Warnings = validation.Warnings
                };

                _logger.LogInformation("Test orchestration completed: {Passed}/{Total} tests passed in {Duration}ms (Speedup: {Speedup:F2}x)",
                    passedTests, testResults.Count, totalExecutionTime.TotalMilliseconds, parallelMetrics.SpeedupFactor);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test orchestration");
                return CreateFailedResult("Orchestration failed", new List<string> { ex.Message });
            }
            finally
            {
                stopwatch.Stop();
            }
        }
    }
}
