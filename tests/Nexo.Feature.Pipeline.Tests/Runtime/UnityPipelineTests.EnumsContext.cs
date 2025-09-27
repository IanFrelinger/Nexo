using System;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Enums and context tests for Unity pipeline.
    /// </summary>
    public partial class UnityPipelineTests
    {
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
    }
}
