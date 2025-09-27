using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Detection and models tests for Unity pipeline.
    /// </summary>
    public partial class UnityPipelineTests
    {
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
    }
}
