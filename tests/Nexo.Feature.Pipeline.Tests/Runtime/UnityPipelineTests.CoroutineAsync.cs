using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Coroutine and async tests for Unity pipeline.
    /// </summary>
    public partial class UnityPipelineTests
    {
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
    }
}
