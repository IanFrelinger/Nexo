using System;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Xunit;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Error handling and serialization tests for Unity pipeline.
    /// </summary>
    public partial class UnityPipelineTests
    {
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
                        var context = new PipelineContext(null!, null!);
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
                        AssertRuntimeCondition(deserializedConfig!.Name == config.Name, 
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
    }
}
