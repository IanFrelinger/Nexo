using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services;
using Nexo.Feature.Pipeline.Tests.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Cross-runtime tests for the Pipeline feature.
    /// Tests functionality across different runtime environments (.NET, Unity, Mono, etc.).
    /// </summary>
    public class CrossRuntimePipelineTests : CrossRuntimeTestBase
    {
        public CrossRuntimePipelineTests(ITestOutputHelper testOutput) : base(testOutput)
        {
        }

        [RuntimeFact(RuntimeDetection.RuntimeType.DotNet, RuntimeDetection.RuntimeType.CoreCLR)]
        public void Pipeline_CrossRuntime_Detection_Works()
        {
            Logger.LogInformation("Testing runtime detection across different environments");
            
            var runtimeInfo = RuntimeDetection.GetRuntimeInfo();
            Logger.LogInformation($"Current runtime info: {runtimeInfo}");
            
            AssertRuntimeCondition(CurrentRuntime != RuntimeDetection.RuntimeType.Unknown, 
                "Runtime should be detected correctly");
            
            AssertRuntimeCondition(!string.IsNullOrEmpty(RuntimeDetection.RuntimeVersion), 
                "Runtime version should be available");
            
            AssertRuntimeCondition(!string.IsNullOrEmpty(RuntimeDetection.FrameworkDescription), 
                "Framework description should be available");
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Models_WorkCorrectly()
        {
            Logger.LogInformation("Testing Pipeline models across different runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                // Test core model instantiation
                var pipelineConfig = new PipelineConfiguration();
                var commandMetadata = new CommandMetadata();
                var behaviorMetadata = new BehaviorMetadata();
                var aggregatorMetadata = new AggregatorMetadata();
                var executionResult = new PipelineExecutionResult();
                var executionPlan = new PipelineExecutionPlan();
                var executionStep = new PipelineExecutionStep();
                var executionPhase = new PipelineExecutionPhase();
                
                AssertRuntimeCondition(pipelineConfig != null, "PipelineConfiguration should be instantiable");
                AssertRuntimeCondition(commandMetadata != null, "CommandMetadata should be instantiable");
                AssertRuntimeCondition(behaviorMetadata != null, "BehaviorMetadata should be instantiable");
                AssertRuntimeCondition(aggregatorMetadata != null, "AggregatorMetadata should be instantiable");
                AssertRuntimeCondition(executionResult != null, "PipelineExecutionResult should be instantiable");
                AssertRuntimeCondition(executionPlan != null, "PipelineExecutionPlan should be instantiable");
                AssertRuntimeCondition(executionStep != null, "PipelineExecutionStep should be instantiable");
                AssertRuntimeCondition(executionPhase != null, "PipelineExecutionPhase should be instantiable");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Enums_WorkCorrectly()
        {
            Logger.LogInformation("Testing Pipeline enums across different runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                // Test enum values
                var commandCategories = Enum.GetValues(typeof(CommandCategory));
                var behaviorStrategies = Enum.GetValues(typeof(BehaviorExecutionStrategy));
                var aggregatorStrategies = Enum.GetValues(typeof(AggregatorExecutionStrategy));
                var executionStatuses = Enum.GetValues(typeof(ExecutionStatus));
                var commandPriorities = Enum.GetValues(typeof(CommandPriority));
                
                AssertRuntimeCondition(commandCategories.Length > 0, "CommandCategory should have values");
                AssertRuntimeCondition(behaviorStrategies.Length > 0, "BehaviorExecutionStrategy should have values");
                AssertRuntimeCondition(aggregatorStrategies.Length > 0, "AggregatorExecutionStrategy should have values");
                AssertRuntimeCondition(executionStatuses.Length > 0, "ExecutionStatus should have values");
                AssertRuntimeCondition(commandPriorities.Length > 0, "CommandPriority should have values");
                
                // Test specific enum values
                AssertRuntimeCondition(Enum.IsDefined(typeof(CommandCategory), CommandCategory.Analysis), 
                    "CommandCategory.Analysis should be defined");
                AssertRuntimeCondition(Enum.IsDefined(typeof(BehaviorExecutionStrategy), BehaviorExecutionStrategy.Sequential), 
                    "BehaviorExecutionStrategy.Sequential should be defined");
                AssertRuntimeCondition(Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Parallel), 
                    "AggregatorExecutionStrategy.Parallel should be defined");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_ExecutionContext_WorksCorrectly()
        {
            Logger.LogInformation("Testing Pipeline execution context across different runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                var configuration = new PipelineConfiguration();
                var context = new PipelineContext(Logger, configuration);
                
                // Test context property assignments
                context.Status = PipelineExecutionStatus.Executing;
                context.Status = PipelineExecutionStatus.Completed;
                
                // Test execution result creation
                var result = new PipelineExecutionResult
                {
                    ExecutionId = context.ExecutionId,
                    Status = ExecutionStatus.Completed,
                    StartTime = context.StartTime,
                    EndTime = DateTime.UtcNow,
                    IsSuccess = true
                };
                
                AssertRuntimeCondition(!string.IsNullOrEmpty(context.ExecutionId), 
                    "Context should have a valid ExecutionId");
                AssertRuntimeCondition(context.StartTime != default, 
                    "Context should have a valid StartTime");
                AssertRuntimeCondition(context.Status == PipelineExecutionStatus.Completed, 
                    "Context status should be set correctly");
                AssertRuntimeCondition(result.ExecutionId == context.ExecutionId, 
                    "Result should reference the correct execution");
                AssertRuntimeCondition(result.IsSuccess, 
                    "Result should indicate success");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Configuration_WorksCorrectly()
        {
            Logger.LogInformation("Testing Pipeline configuration across different runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                var config = new PipelineConfiguration();
                
                // Test interface implementation
                AssertRuntimeCondition(config.MaxParallelExecutions > 0, 
                    "MaxParallelExecutions should be positive");
                AssertRuntimeCondition(config.CommandTimeoutMs > 0, 
                    "CommandTimeoutMs should be positive");
                AssertRuntimeCondition(config.BehaviorTimeoutMs > 0, 
                    "BehaviorTimeoutMs should be positive");
                AssertRuntimeCondition(config.AggregatorTimeoutMs > 0, 
                    "AggregatorTimeoutMs should be positive");
                
                // Test configuration methods
                config.SetValue("testKey", "testValue");
                var retrievedValue = config.GetValue<string>("testKey");
                AssertRuntimeCondition(retrievedValue == "testValue", 
                    "Configuration should store and retrieve values correctly");
                
                AssertRuntimeCondition(config.HasKey("testKey"), 
                    "Configuration should detect existing keys");
                AssertRuntimeCondition(!config.HasKey("nonexistentKey"), 
                    "Configuration should not detect non-existent keys");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Validation_WorksCorrectly()
        {
            Logger.LogInformation("Testing Pipeline validation across different runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                var command = new PipelineValidationCommand(NullLogger<PipelineValidationCommand>.Instance);
                
                // Test interface validation
                var interfaceResult = command.ValidatePipelineInterfaces(timeoutMs: 3000);
                AssertRuntimeCondition(interfaceResult, 
                    "Pipeline interfaces should be valid across runtimes");
                
                // Test enum validation
                var enumResult = command.ValidatePipelineEnums(timeoutMs: 3000);
                AssertRuntimeCondition(enumResult, 
                    "Pipeline enums should be valid across runtimes");
                
                // Test model validation
                var modelResult = command.ValidatePipelineModels(timeoutMs: 3000);
                AssertRuntimeCondition(modelResult, 
                    "Pipeline models should be valid across runtimes");
                
                // Test execution context validation
                var contextResult = command.ValidatePipelineExecutionContext(timeoutMs: 3000);
                AssertRuntimeCondition(contextResult, 
                    "Pipeline execution context should be valid across runtimes");
            });
        }

        [RuntimeTheory]
        [InlineData("async")]
        [InlineData("reflection")]
        [InlineData("linq")]
        [InlineData("json")]
        [InlineData("serialization")]
        public void Pipeline_CrossRuntime_Feature_Support_Works(string feature)
        {
            Logger.LogInformation($"Testing feature support for '{feature}' across runtimes");
            
            var supportsFeature = RuntimeSupportsFeature(feature);
            Logger.LogInformation($"Runtime {CurrentRuntime} supports feature '{feature}': {supportsFeature}");
            
            // All runtimes should support basic features
            if (feature == "async" || feature == "reflection" || feature == "linq" || feature == "json" || feature == "serialization")
            {
                AssertRuntimeCondition(supportsFeature, 
                    $"Feature '{feature}' should be supported on runtime {CurrentRuntime}");
            }
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Performance_Characteristics()
        {
            Logger.LogInformation("Testing Pipeline performance characteristics across runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                var startTime = DateTime.UtcNow;
                
                // Perform some pipeline operations
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
                Logger.LogInformation($"Pipeline operations completed in {elapsed.TotalMilliseconds:F2}ms on {CurrentRuntime}");
                
                // Performance expectations vary by runtime
                var maxExpectedTime = GetRuntimeAdjustedTimeout(100); // 100ms base
                AssertRuntimeCondition(elapsed.TotalMilliseconds < maxExpectedTime, 
                    $"Pipeline operations should complete within {maxExpectedTime}ms on {CurrentRuntime}");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Memory_Usage()
        {
            Logger.LogInformation("Testing Pipeline memory usage across runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                var initialMemory = GC.GetTotalMemory(false);
                
                // Create pipeline objects
                var configs = new List<PipelineConfiguration>();
                var contexts = new List<PipelineContext>();
                var results = new List<PipelineExecutionResult>();
                
                for (int i = 0; i < 100; i++)
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
                
                Logger.LogInformation($"Memory usage increased by {memoryIncrease} bytes on {CurrentRuntime}");
                
                // Memory usage should be reasonable (less than 1MB for 100 objects)
                var maxExpectedMemory = 1024 * 1024; // 1MB
                AssertRuntimeCondition(memoryIncrease < maxExpectedMemory, 
                    $"Memory usage should be reasonable on {CurrentRuntime}");
                
                // Clean up
                configs.Clear();
                contexts.Clear();
                results.Clear();
                GC.Collect();
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Concurrent_Operations()
        {
            Logger.LogInformation("Testing Pipeline concurrent operations across runtimes");
            
            SkipIfFeatureNotSupported("threading", "Threading support required for concurrent operations");
            
            RunWithRuntimeTimeout(() =>
            {
                var tasks = new List<Task<bool>>();
                var results = new List<bool>();
                
                // Create concurrent pipeline operations
                for (int i = 0; i < 10; i++)
                {
                    var task = Task.Run(() =>
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
                        
                        return !string.IsNullOrEmpty(result.ExecutionId);
                    });
                    
                    tasks.Add(task);
                }
                
                // Wait for all tasks to complete
                Task.WaitAll(tasks.ToArray());
                
                // Collect results
                foreach (var task in tasks)
                {
                    results.Add(task.Result);
                }
                
                AssertRuntimeCondition(results.All(r => r), 
                    "All concurrent pipeline operations should succeed");
                AssertRuntimeCondition(results.Count == 10, 
                    "All 10 concurrent operations should complete");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Error_Handling()
        {
            Logger.LogInformation("Testing Pipeline error handling across runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                // Test null reference handling
                var nullRefException = AssertRuntimeException<ArgumentNullException>(() =>
                {
                    var context = new PipelineContext(null, null);
                });
                
                AssertRuntimeCondition(nullRefException != null, 
                    "Should throw ArgumentNullException for null parameters");
                
                // Test invalid configuration handling
                var config = new PipelineConfiguration();
                var context = new PipelineContext(Logger, config);
                
                // Test setting invalid status (this should work)
                context.Status = PipelineExecutionStatus.Failed;
                AssertRuntimeCondition(context.Status == PipelineExecutionStatus.Failed, 
                    "Should be able to set status to Failed");
            });
        }

        [RuntimeTimeout(5000)]
        public void Pipeline_CrossRuntime_Serialization_Compatibility()
        {
            Logger.LogInformation("Testing Pipeline serialization compatibility across runtimes");
            
            RunWithRuntimeTimeout(() =>
            {
                var config = new PipelineConfiguration
                {
                    Name = "Test Pipeline",
                    Version = "1.0.0",
                    Description = "Test pipeline for cross-runtime compatibility"
                };
                
                // Test JSON serialization
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(config);
                    AssertRuntimeCondition(!string.IsNullOrEmpty(json), 
                        "Configuration should serialize to JSON");
                    
                    var deserializedConfig = System.Text.Json.JsonSerializer.Deserialize<PipelineConfiguration>(json);
                    AssertRuntimeCondition(deserializedConfig != null, 
                        "Configuration should deserialize from JSON");
                    AssertRuntimeCondition(deserializedConfig.Name == config.Name, 
                        "Deserialized configuration should match original");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"JSON serialization failed on {CurrentRuntime}: {ex.Message}");
                    // Some runtimes might not support full JSON serialization
                    AssertRuntimeCondition(CurrentRuntime == RuntimeDetection.RuntimeType.Unity, 
                        "JSON serialization should work on non-Unity runtimes");
                }
            });
        }
    }
} 