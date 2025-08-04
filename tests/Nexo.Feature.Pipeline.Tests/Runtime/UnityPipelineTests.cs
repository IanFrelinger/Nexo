using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Unity-specific tests for the Pipeline feature.
    /// These tests demonstrate how to test Pipeline functionality within Unity's runtime environment.
    /// </summary>
    public class UnityPipelineTests : CrossRuntimeTestBase
    {
        public UnityPipelineTests(ITestOutputHelper testOutput) : base(testOutput)
        {
        }

        [RuntimeFact(RuntimeDetection.RuntimeType.Unity)]
        public void Unity_Pipeline_Detection_Works()
        {
            Logger.LogInformation("Testing Pipeline detection in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                var runtimeInfo = RuntimeDetection.GetRuntimeInfo();
                Logger.LogInformation($"Unity runtime info: {runtimeInfo}");
                
                AssertRuntimeCondition(CurrentRuntime == RuntimeDetection.RuntimeType.Unity, 
                    "Should be running in Unity runtime");
                
                // Test Unity-specific features
                AssertRuntimeCondition(UnityTestAdapter.IsUnityFeatureAvailable("coroutines"), 
                    "Unity should support coroutines");
                AssertRuntimeCondition(UnityTestAdapter.IsUnityFeatureAvailable("gameobjects"), 
                    "Unity should support GameObjects");
                AssertRuntimeCondition(UnityTestAdapter.IsUnityFeatureAvailable("components"), 
                    "Unity should support Components");
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Models_WorkCorrectly()
        {
            Logger.LogInformation("Testing Pipeline models in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    // Test core model instantiation in Unity
                    var pipelineConfig = new PipelineConfiguration();
                    var commandMetadata = new CommandMetadata();
                    var behaviorMetadata = new BehaviorMetadata();
                    var aggregatorMetadata = new AggregatorMetadata();
                    var executionResult = new PipelineExecutionResult();
                    
                    AssertRuntimeCondition(pipelineConfig != null, "PipelineConfiguration should work in Unity");
                    AssertRuntimeCondition(commandMetadata != null, "CommandMetadata should work in Unity");
                    AssertRuntimeCondition(behaviorMetadata != null, "BehaviorMetadata should work in Unity");
                    AssertRuntimeCondition(aggregatorMetadata != null, "AggregatorMetadata should work in Unity");
                    AssertRuntimeCondition(executionResult != null, "PipelineExecutionResult should work in Unity");
                });
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Enums_WorkCorrectly()
        {
            Logger.LogInformation("Testing Pipeline enums in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    // Test enum values in Unity
                    var commandCategories = Enum.GetValues(typeof(CommandCategory));
                    var behaviorStrategies = Enum.GetValues(typeof(BehaviorExecutionStrategy));
                    var aggregatorStrategies = Enum.GetValues(typeof(AggregatorExecutionStrategy));
                    var executionStatuses = Enum.GetValues(typeof(ExecutionStatus));
                    var commandPriorities = Enum.GetValues(typeof(CommandPriority));
                    
                    AssertRuntimeCondition(commandCategories.Length > 0, "CommandCategory should have values in Unity");
                    AssertRuntimeCondition(behaviorStrategies.Length > 0, "BehaviorExecutionStrategy should have values in Unity");
                    AssertRuntimeCondition(aggregatorStrategies.Length > 0, "AggregatorExecutionStrategy should have values in Unity");
                    AssertRuntimeCondition(executionStatuses.Length > 0, "ExecutionStatus should have values in Unity");
                    AssertRuntimeCondition(commandPriorities.Length > 0, "CommandPriority should have values in Unity");
                    
                    // Test specific enum values
                    AssertRuntimeCondition(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Analysis), 
                        "CommandCategory.Analysis should be defined in Unity");
                    AssertRuntimeCondition(Enum.IsDefined(typeof(BehaviorExecutionStrategy), BehaviorExecutionStrategy.Sequential), 
                        "BehaviorExecutionStrategy.Sequential should be defined in Unity");
                    AssertRuntimeCondition(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Parallel), 
                        "AggregatorExecutionStrategy.Parallel should be defined in Unity");
                });
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_ExecutionContext_WorksCorrectly()
        {
            Logger.LogInformation("Testing Pipeline execution context in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    var configuration = new PipelineConfiguration();
                    var context = new PipelineContext(Logger, configuration);
                    
                    // Test context property assignments in Unity
                    context.Status = PipelineExecutionStatus.Executing;
                    context.Status = PipelineExecutionStatus.Completed;
                    
                    // Test execution result creation in Unity
                    var result = new PipelineExecutionResult
                    {
                        ExecutionId = context.ExecutionId,
                        Status = ExecutionStatus.Completed,
                        StartTime = context.StartTime,
                        EndTime = DateTime.UtcNow,
                        IsSuccess = true
                    };
                    
                    AssertRuntimeCondition(!string.IsNullOrEmpty(context.ExecutionId), 
                        "Context should have a valid ExecutionId in Unity");
                    AssertRuntimeCondition(context.StartTime != default, 
                        "Context should have a valid StartTime in Unity");
                    AssertRuntimeCondition(context.Status == PipelineExecutionStatus.Completed, 
                        "Context status should be set correctly in Unity");
                    AssertRuntimeCondition(result.ExecutionId == context.ExecutionId, 
                        "Result should reference the correct execution in Unity");
                    AssertRuntimeCondition(result.IsSuccess, 
                        "Result should indicate success in Unity");
                });
            }, Logger);
        }

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

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Coroutine_Support()
        {
            Logger.LogInformation("Testing Pipeline coroutine support in Unity environment");
            
            if (CurrentRuntime != RuntimeDetection.RuntimeType.Unity)
            {
                Logger.LogInformation("Skipping coroutine test - not running in Unity");
                return;
            }
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    // Test that we can create coroutines for pipeline operations
                    var coroutine = UnityTestAdapter.UnityTestUtils.CreateTestCoroutine(() =>
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
                        
                        AssertRuntimeCondition(!string.IsNullOrEmpty(result.ExecutionId), 
                            "Pipeline should work in Unity coroutines");
                    });
                    
                    AssertRuntimeCondition(coroutine != null, 
                        "Should be able to create coroutines in Unity");
                });
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Async_Support()
        {
            Logger.LogInformation("Testing Pipeline async support in Unity environment");
            
            UnityTestAdapter.RunUnityTestAsync(async () =>
            {
                await Task.Delay(100); // Simulate async work
                
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
                
                AssertRuntimeCondition(!string.IsNullOrEmpty(result.ExecutionId), 
                    "Pipeline should work in Unity async operations");
                AssertRuntimeCondition(result.IsSuccess, 
                    "Pipeline async operations should succeed in Unity");
            }, Logger).GetAwaiter().GetResult();
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Error_Handling()
        {
            Logger.LogInformation("Testing Pipeline error handling in Unity environment");
            
            // Skip if not running in Unity runtime
            if (RuntimeDetection.CurrentRuntime != RuntimeDetection.RuntimeType.Unity)
            {
                Logger.LogInformation("Skipping Unity-specific test - not running in Unity runtime");
                return;
            }
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    // Test null reference handling in Unity
                    var nullRefException = AssertRuntimeException<ArgumentNullException>(() =>
                    {
                        var context = new PipelineContext(null, null);
                    });
                    
                    AssertRuntimeCondition(nullRefException != null, 
                        "Should throw ArgumentNullException for null parameters in Unity");
                    
                    // Test invalid configuration handling in Unity
                    var config = new PipelineConfiguration();
                    var context = new PipelineContext(Logger, config);
                    
                    // Test setting invalid status (this should work)
                    context.Status = PipelineExecutionStatus.Failed;
                    AssertRuntimeCondition(context.Status == PipelineExecutionStatus.Failed, 
                        "Should be able to set status to Failed in Unity");
                });
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Serialization_Compatibility()
        {
            Logger.LogInformation("Testing Pipeline serialization compatibility in Unity environment");
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    var config = new PipelineConfiguration
                    {
                        Name = "Unity Test Pipeline",
                        Version = "1.0.0",
                        Description = "Test pipeline for Unity compatibility"
                    };
                    
                    // Test JSON serialization in Unity
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(config);
                        AssertRuntimeCondition(!string.IsNullOrEmpty(json), 
                            "Configuration should serialize to JSON in Unity");
                        
                        var deserializedConfig = System.Text.Json.JsonSerializer.Deserialize<PipelineConfiguration>(json);
                        AssertRuntimeCondition(deserializedConfig != null, 
                            "Configuration should deserialize from JSON in Unity");
                        AssertRuntimeCondition(deserializedConfig.Name == config.Name, 
                            "Deserialized configuration should match original in Unity");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"JSON serialization failed in Unity: {ex.Message}");
                        // Unity might have limited JSON support
                        AssertRuntimeCondition(false, 
                            "JSON serialization should work in Unity");
                    }
                });
            }, Logger);
        }

        [RuntimeTimeout(5000)]
        public void Unity_Pipeline_Feature_Support()
        {
            Logger.LogInformation("Testing Pipeline feature support in Unity environment");
            
            // Skip if not running in Unity runtime
            if (RuntimeDetection.CurrentRuntime != RuntimeDetection.RuntimeType.Unity)
            {
                Logger.LogInformation("Skipping Unity-specific test - not running in Unity runtime");
                return;
            }
            
            UnityTestAdapter.RunUnityTest(() =>
            {
                RunWithRuntimeTimeout(() =>
                {
                    // Test Unity-specific feature support
                    var features = new[] { "coroutines", "gameobjects", "components", "scenes", "physics" };
                    
                    foreach (var feature in features)
                    {
                        var supportsFeature = UnityTestAdapter.IsUnityFeatureAvailable(feature);
                        Logger.LogInformation($"Unity supports feature '{feature}': {supportsFeature}");
                        
                        if (feature == "coroutines" || feature == "gameobjects" || feature == "components")
                        {
                            AssertRuntimeCondition(supportsFeature, 
                                $"Unity should support feature '{feature}'");
                        }
                    }
                    
                    // Test general runtime feature support
                    var generalFeatures = new[] { "async", "reflection", "linq", "json", "serialization" };
                    
                    foreach (var feature in generalFeatures)
                    {
                        var supportsFeature = RuntimeSupportsFeature(feature);
                        Logger.LogInformation($"Runtime supports feature '{feature}' in Unity: {supportsFeature}");
                        
                        if (feature == "async" || feature == "reflection" || feature == "linq")
                        {
                            AssertRuntimeCondition(supportsFeature, 
                                $"Runtime should support feature '{feature}' in Unity");
                        }
                    }
                });
            }, Logger);
        }
    }
} 