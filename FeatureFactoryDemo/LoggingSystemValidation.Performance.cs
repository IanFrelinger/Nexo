using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Performance and concurrency tests
    /// </summary>
    public partial class LoggingSystemValidation
    {
        private async Task<TestResult> TestPerformanceAsync()
        {
            _logger.LogInformation("Search Testing logging performance...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var iterations = 1000;
                var stopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < iterations; i++)
                {
                    testService.Logger.LogInformation("Performance test message {Iteration}", i);
                }
                stopwatch.Stop();
                
                var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
                var logsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
                
                if (averageTime > 0.1 || logsPerSecond < 1000)
                {
                    return new TestResult { Success = false, Message = $"Performance too slow: {averageTime:F4}ms per log, {logsPerSecond:F0} logs/sec" };
                }

                return new TestResult { Success = true, Message = $"Performance acceptable: {averageTime:F4}ms per log, {logsPerSecond:F0} logs/sec" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Performance test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestConcurrentOperationsAsync()
        {
            _logger.LogInformation("Search Testing concurrent operations...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var iterations = 100;
                var concurrentTasks = 10;
                var stopwatch = Stopwatch.StartNew();
                
                var tasks = new List<Task>();
                for (int task = 0; task < concurrentTasks; task++)
                {
                    int taskId = task;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            testService.Logger.LogInformation("Concurrent test message {TaskId} {Iteration}", taskId, i);
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
                stopwatch.Stop();
                
                var totalLogs = iterations * concurrentTasks;
                var logsPerSecond = totalLogs / stopwatch.Elapsed.TotalSeconds;
                
                if (logsPerSecond < 100)
                {
                    return new TestResult { Success = false, Message = $"Concurrent performance too slow: {logsPerSecond:F0} logs/sec" };
                }

                return new TestResult { Success = true, Message = $"Concurrent operations working: {logsPerSecond:F0} logs/sec" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Concurrent operations test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestMemoryUsageAsync()
        {
            _logger.LogInformation("Search Testing memory usage...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var iterations = 1000;
                var initialMemory = GC.GetTotalMemory(true);
                
                for (int i = 0; i < iterations; i++)
                {
                    testService.Logger.LogInformation("Memory test message {Iteration} with additional data", i);
                }
                
                var finalMemory = GC.GetTotalMemory(false);
                var memoryIncrease = finalMemory - initialMemory;
                var averageMemoryPerLog = memoryIncrease / (double)iterations;
                
                if (averageMemoryPerLog > 1000)
                {
                    return new TestResult { Success = false, Message = $"Memory usage too high: {averageMemoryPerLog:F2} bytes per log" };
                }

                return new TestResult { Success = true, Message = $"Memory usage acceptable: {averageMemoryPerLog:F2} bytes per log" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Memory usage test failed: {ex.Message}" };
            }
        }
    }
}
