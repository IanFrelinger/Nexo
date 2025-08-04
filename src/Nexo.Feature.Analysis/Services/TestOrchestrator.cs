using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Intelligent test orchestrator that provides parallel execution, dependency management, and incremental testing.
    /// </summary>
    public class TestOrchestrator : ITestOrchestrator
    {
        private readonly ILogger<TestOrchestrator> _logger;
        private readonly ISmartTestSelector _smartTestSelector;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IResourceOptimizer _resourceOptimizer;
        private readonly ITestDependencyAnalyzer _testDependencyAnalyzer;
        private readonly ITestExecutionEngine _testExecutionEngine;
        private readonly ConcurrentDictionary<string, TestExecutionResult> _cachedResults = new ConcurrentDictionary<string, TestExecutionResult>();

        public TestOrchestrator(
            ILogger<TestOrchestrator> logger,
            ISmartTestSelector smartTestSelector,
            IResourceMonitor resourceMonitor,
            IResourceOptimizer resourceOptimizer,
            ITestDependencyAnalyzer testDependencyAnalyzer,
            ITestExecutionEngine testExecutionEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _smartTestSelector = smartTestSelector ?? throw new ArgumentNullException(nameof(smartTestSelector));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _resourceOptimizer = resourceOptimizer ?? throw new ArgumentNullException(nameof(resourceOptimizer));
            _testDependencyAnalyzer = testDependencyAnalyzer ?? throw new ArgumentNullException(nameof(testDependencyAnalyzer));
            _testExecutionEngine = testExecutionEngine ?? throw new ArgumentNullException(nameof(testExecutionEngine));
        }

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

        public async Task<ParallelExecutionResult> ExecuteTestsInParallelAsync(List<string> testFiles, ParallelExecutionOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Executing {Count} tests in parallel with max parallelism: {MaxParallelism}", testFiles.Count, options.MaxParallelism);

            try
            {
                var results = new ConcurrentBag<TestExecutionResult>();
                var semaphore = new SemaphoreSlim(options.MaxParallelism, options.MaxParallelism);
                var tasks = new List<Task>();

                foreach (var testFile in testFiles)
                {
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            var testStopwatch = Stopwatch.StartNew();
                            var result = await _testExecutionEngine.ExecuteTestAsync(testFile, options.TestTimeout, cancellationToken);
                            testStopwatch.Stop();

                            result.ExecutionTime = testStopwatch.Elapsed;
                            result.WasExecutedInParallel = true;
                            results.Add(result);

                            if (!options.ContinueOnFailure && !result.IsSuccess)
                            {
                                _logger.LogError("Test {TestFile} failed, stopping parallel execution", testFile);
                                throw new OperationCanceledException($"Test {testFile} failed");
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                var totalTime = stopwatch.Elapsed;
                var metrics = new ParallelExecutionMetrics
                {
                    TotalTime = totalTime,
                    MaxParallelism = options.MaxParallelism,
                    AverageParallelism = CalculateAverageParallelism(results.ToList()),
                    SequentialTime = TimeSpan.FromMilliseconds(results.Sum(r => r.ExecutionTime.TotalMilliseconds)),
                    SpeedupFactor = results.Sum(r => r.ExecutionTime.TotalMilliseconds) / totalTime.TotalMilliseconds,
                    Efficiency = (results.Sum(r => r.ExecutionTime.TotalMilliseconds) / totalTime.TotalMilliseconds) / options.MaxParallelism
                };

                var parallelResult = new ParallelExecutionResult
                {
                    IsSuccess = results.All(r => r.IsSuccess),
                    TestsExecuted = results.Count,
                    Metrics = metrics,
                    Results = results.ToList()
                };

                _logger.LogInformation("Parallel execution completed: {Executed} tests in {Duration}ms (Speedup: {Speedup:F2}x)",
                    results.Count, totalTime.TotalMilliseconds, metrics.SpeedupFactor);

                return parallelResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in parallel test execution");
                return new ParallelExecutionResult
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

        public async Task<TestExecutionPlan> CreateDependencyOrderedPlanAsync(List<string> testFiles, DependencyOrderingOptions options, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating dependency-ordered execution plan for {Count} test files", testFiles.Count);

            try
            {
                var dependencies = new List<TestDependencyRule>();

                // Auto-detect dependencies if enabled
                if (options.AutoDetectDependencies)
                {
                    var detectedDependencies = await _testDependencyAnalyzer.AnalyzeDependenciesAsync(testFiles, cancellationToken);
                    dependencies.AddRange(detectedDependencies);
                }

                // Add custom dependencies
                dependencies.AddRange(options.CustomDependencies);

                // Validate for cycles if enabled
                if (options.ValidateCycles)
                {
                    var cycles = DetectDependencyCycles(dependencies);
                    if (cycles.Any())
                    {
                        return new TestExecutionPlan
                        {
                            IsValid = false,
                            ValidationErrors = cycles.Select(c => $"Circular dependency detected: {string.Join(" -> ", c)}").ToList()
                        };
                    }
                }

                // Create phases based on dependencies
                var phases = CreatePhasesFromDependencies(testFiles, dependencies, options);

                var totalEstimatedTime = TimeSpan.FromMinutes(phases.Sum(p => p.EstimatedTime.TotalMinutes));
                var dependencyGraph = GenerateDependencyGraph(dependencies);

                var plan = new TestExecutionPlan
                {
                    Phases = phases,
                    TotalTests = testFiles.Count,
                    EstimatedExecutionTime = totalEstimatedTime,
                    DependencyGraph = dependencyGraph,
                    IsValid = true
                };

                _logger.LogInformation("Dependency-ordered plan created with {Phases} phases, estimated time: {EstimatedTime}",
                    phases.Count, totalEstimatedTime);

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dependency-ordered plan");
                return new TestExecutionPlan
                {
                    IsValid = false,
                    ValidationErrors = new List<string> { ex.Message }
                };
            }
        }

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

        public async Task<ResourceUtilization> GetResourceUtilizationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cpuUsage = await _resourceMonitor.GetCpuUsageAsync(cancellationToken);
                var memoryInfo = await _resourceMonitor.GetMemoryInfoAsync(cancellationToken);
                var availableCores = Environment.ProcessorCount;

                // Calculate recommended parallelism based on available resources
                var recommendedParallelism = Math.Max(1, Math.Min(availableCores, 
                    (int)(availableCores * (1 - cpuUsage / 100.0))));

                return new ResourceUtilization
                {
                    CpuUsagePercent = cpuUsage,
                    MemoryUsageMB = memoryInfo.UsedBytes / (1024 * 1024),
                    AvailableMemoryMB = memoryInfo.AvailableBytes / (1024 * 1024),
                    AvailableCores = availableCores,
                    RecommendedMaxParallelism = recommendedParallelism,
                    IsResourceConstrained = cpuUsage > 80 || memoryInfo.UsagePercentage > 80
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resource utilization");
                return new ResourceUtilization
                {
                    CpuUsagePercent = 0,
                    MemoryUsageMB = 0,
                    AvailableMemoryMB = 0,
                    AvailableCores = Environment.ProcessorCount,
                    RecommendedMaxParallelism = Environment.ProcessorCount,
                    IsResourceConstrained = false
                };
            }
        }

        public TestOrchestrationValidation ValidateOptions(TestOrchestrationOptions options)
        {
            var validation = new TestOrchestrationValidation { IsValid = true };

            if (options.MaxParallelism <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxParallelism must be greater than 0");
            }

            if (options.MaxParallelism > Environment.ProcessorCount * 2)
            {
                validation.Warnings.Add($"MaxParallelism ({options.MaxParallelism}) is higher than recommended ({Environment.ProcessorCount})");
            }

            if (options.MaxMemoryUsageMB <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxMemoryUsageMB must be greater than 0");
            }

            if (options.MaxCpuUsagePercent <= 0 || options.MaxCpuUsagePercent > 100)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxCpuUsagePercent must be between 1 and 100");
            }

            if (options.TestTimeoutSeconds <= 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("TestTimeoutSeconds must be greater than 0");
            }

            if (options.MaxRetryAttempts < 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("MaxRetryAttempts must be non-negative");
            }

            return validation;
        }

        #region Private Methods

        private void AdjustOptionsForResources(TestOrchestrationOptions options, ResourceUtilization utilization)
        {
            if (utilization.IsResourceConstrained)
            {
                var originalParallelism = options.MaxParallelism;
                options.MaxParallelism = Math.Min(options.MaxParallelism, utilization.RecommendedMaxParallelism);
                
                if (options.MaxParallelism != originalParallelism)
                {
                    _logger.LogWarning("Reduced max parallelism from {Original} to {Adjusted} due to resource constraints",
                        originalParallelism, options.MaxParallelism);
                }
            }
        }

        private Task<List<string>> GetAllTestFilesAsync(CancellationToken cancellationToken)
        {
            // This would typically scan the project for test files
            // For now, return a placeholder list
            return Task.FromResult(new List<string>
            {
                "tests/Nexo.Feature.AI.Tests",
                "tests/Nexo.Feature.Analysis.Tests",
                "tests/Nexo.Infrastructure.Tests"
            });
        }

        private List<string> FilterTestsByCategories(List<string> testFiles, TestOrchestrationOptions options)
        {
            // This would filter tests based on categories
            // For now, return all test files
            return testFiles;
        }

        private TestExecutionPlan CreateSimplePlan(List<string> testFiles, TestOrchestrationOptions options)
        {
            var phase = new TestExecutionPhase
            {
                Id = "phase-1",
                Name = "All Tests",
                TestFiles = testFiles,
                CanRunInParallel = options.UseParallelExecution,
                EstimatedTime = TimeSpan.FromMinutes(testFiles.Count * 2) // Rough estimate
            };

            return new TestExecutionPlan
            {
                Phases = new List<TestExecutionPhase> { phase },
                TotalTests = testFiles.Count,
                EstimatedExecutionTime = phase.EstimatedTime,
                IsValid = true
            };
        }

        private async Task<List<TestExecutionResult>> ExecutePhaseAsync(TestExecutionPhase phase, TestOrchestrationOptions options, CancellationToken cancellationToken)
        {
            if (phase.CanRunInParallel)
            {
                var parallelOptions = new ParallelExecutionOptions
                {
                    MaxParallelism = options.MaxParallelism,
                    TestTimeout = TimeSpan.FromSeconds(options.TestTimeoutSeconds),
                    ContinueOnFailure = !options.StopOnFirstFailure
                };

                var parallelResult = await ExecuteTestsInParallelAsync(phase.TestFiles, parallelOptions, cancellationToken);
                return parallelResult.Results;
            }
            else
            {
                var results = new List<TestExecutionResult>();
                foreach (var testFile in phase.TestFiles)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var result = await _testExecutionEngine.ExecuteTestAsync(testFile, TimeSpan.FromSeconds(options.TestTimeoutSeconds), cancellationToken);
                    result.ExecutionPhase = phase.Id;
                    results.Add(result);

                    if (options.StopOnFirstFailure && !result.IsSuccess)
                    {
                        break;
                    }
                }
                return results;
            }
        }

        private List<List<string>> DetectDependencyCycles(List<TestDependencyRule> dependencies)
        {
            var cycles = new List<List<string>>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency.DependentTest))
                {
                    var cycle = new List<string>();
                    if (HasCycle(dependency.DependentTest, dependencies, visited, recursionStack, cycle))
                    {
                        cycles.Add(new List<string>(cycle));
                    }
                }
            }

            return cycles;
        }

        private bool HasCycle(string test, List<TestDependencyRule> dependencies, HashSet<string> visited, HashSet<string> recursionStack, List<string> cycle)
        {
            if (recursionStack.Contains(test))
            {
                cycle.Add(test);
                return true;
            }

            if (visited.Contains(test))
            {
                return false;
            }

            visited.Add(test);
            recursionStack.Add(test);
            cycle.Add(test);

            var dependents = dependencies.Where(d => d.DependencyTest == test).Select(d => d.DependentTest);
            foreach (var dependent in dependents)
            {
                if (HasCycle(dependent, dependencies, visited, recursionStack, cycle))
                {
                    return true;
                }
            }

            recursionStack.Remove(test);
            cycle.RemoveAt(cycle.Count - 1);
            return false;
        }

        private List<TestExecutionPhase> CreatePhasesFromDependencies(List<string> testFiles, List<TestDependencyRule> dependencies, DependencyOrderingOptions options)
        {
            var phases = new List<TestExecutionPhase>();
            var remainingTests = new HashSet<string>(testFiles);
            var phaseNumber = 1;

            while (remainingTests.Any())
            {
                var independentTests = GetIndependentTests(remainingTests, dependencies);
                var phaseTests = independentTests.Take(options.MaxGroupSize).ToList();

                var phase = new TestExecutionPhase
                {
                    Id = $"phase-{phaseNumber}",
                    Name = $"Phase {phaseNumber}",
                    TestFiles = phaseTests,
                    CanRunInParallel = options.GroupIndependentTests,
                    EstimatedTime = TimeSpan.FromMinutes(phaseTests.Count * 2) // Rough estimate
                };

                phases.Add(phase);

                foreach (var test in phaseTests)
                {
                    remainingTests.Remove(test);
                }

                phaseNumber++;
            }

            return phases;
        }

        private List<string> GetIndependentTests(HashSet<string> remainingTests, List<TestDependencyRule> dependencies)
        {
            var dependentTests = new HashSet<string>(dependencies
                .Where(d => remainingTests.Contains(d.DependentTest))
                .Select(d => d.DependentTest));

            return remainingTests.Where(test => !dependentTests.Contains(test)).ToList();
        }

        private string GenerateDependencyGraph(List<TestDependencyRule> dependencies)
        {
            // Simple text-based dependency graph
            var graph = new List<string>();
            foreach (var dependency in dependencies)
            {
                graph.Add($"{dependency.DependencyTest} -> {dependency.DependentTest}");
            }
            return string.Join("\n", graph);
        }

        private double CalculateAverageParallelism(List<TestExecutionResult> results)
        {
            if (!results.Any()) return 0;

            var parallelResults = results.Where(r => r.WasExecutedInParallel).ToList();
            if (!parallelResults.Any()) return 1.0;

            // This is a simplified calculation - in practice, you'd track actual parallelism over time
            return Math.Min(Environment.ProcessorCount, parallelResults.Count);
        }

        private TestOrchestrationResult CreateFailedResult(string reason, List<string> errors)
        {
            return new TestOrchestrationResult
            {
                IsSuccess = false,
                ErrorMessage = reason,
                Warnings = errors
            };
        }

        private TestOrchestrationResult CreateEmptyResult()
        {
            return new TestOrchestrationResult
            {
                IsSuccess = true,
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                SkippedTests = 0,
                TotalExecutionTime = TimeSpan.Zero
            };
        }

        #endregion
    }
} 