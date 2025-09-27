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
    }
}
