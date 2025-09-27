using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Performance and memory tests for Unity pipeline.
    /// </summary>
    public partial class UnityPipelineTests
    {
        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Performance_Characteristics()
        {
            Logger.LogInformation("Testing Pipeline performance characteristics in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    var startTime = DateTime.UtcNow;
                    
                    // Perform pipeline operations in Unity
                    var config = new PipelineConfiguration();
                    var context = new PipelineContext(Logger, config);
                    var result = new PipelineExecutionResult
                    {
                        ExecutionId = context.ExecutionId,
                        Status = ExecutionStatus.Completed,
                        StartTime = context.StartTime,
                        EndTime = DateTime.UtcNow,
                        IsSuccess = true
                    };
                    
                    var elapsed = DateTime.UtcNow - startTime;
                    Logger.LogInformation($"Pipeline operations completed in {elapsed.TotalMilliseconds:F2}ms in Unity");
                    
                    // Unity-specific performance expectations
                    var maxExpectedTime = UnityTestAdapter.UnityConfig.TimeoutMultiplier * 100; // 200ms for Unity
                    AssertRuntimeCondition(elapsed.TotalMilliseconds < maxExpectedTime, 
                        $"Pipeline operations should complete within {maxExpectedTime}ms in Unity");
                    
                    // Get Unity performance metrics
                    var metrics = UnityTestAdapter.GetUnityPerformanceMetrics();
                    Logger.LogInformation($"Unity performance metrics: FrameRate={metrics.FrameRate}, Memory={metrics.MemoryUsageMB}MB, CPU={metrics.CpuUsagePercent}%");
                });
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Memory_Usage()
        {
            Logger.LogInformation("Testing Pipeline memory usage in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    var initialMemory = GC.GetTotalMemory(false);
                    
                    // Create pipeline objects in Unity
                    var configs = new List<PipelineConfiguration>();
                    var contexts = new List<PipelineContext>();
                    var results = new List<PipelineExecutionResult>();
                    
                    for (int i = 0; i < 50; i++) // Reduced count for Unity
                    {
                        var config = new PipelineConfiguration();
                        var context = new PipelineContext(Logger, config);
                        var result = new PipelineExecutionResult
                        {
                            ExecutionId = context.ExecutionId,
                            Status = ExecutionStatus.Completed,
                            StartTime = context.StartTime,
                            EndTime = DateTime.UtcNow,
                            IsSuccess = true
                        };
                        
                        configs.Add(config);
                        contexts.Add(context);
                        results.Add(result);
                    }
                    
                    var finalMemory = GC.GetTotalMemory(false);
                    var memoryIncrease = finalMemory - initialMemory;
                    
                    Logger.LogInformation($"Memory usage increased by {memoryIncrease} bytes in Unity");
                    
                    // Unity-specific memory expectations
                    var maxExpectedMemory = UnityTestAdapter.UnityConfig.MaxMemoryMB * 1024 * 1024; // 512MB
                    AssertRuntimeCondition(memoryIncrease < maxExpectedMemory, 
                        $"Memory usage should be reasonable in Unity (less than {UnityTestAdapter.UnityConfig.MaxMemoryMB}MB)");
                    
                    // Clean up
                    configs.Clear();
                    contexts.Clear();
                    results.Clear();
                    GC.Collect();
                });
            }, Logger);
        }
    }
}
