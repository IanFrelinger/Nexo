using System;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Enums;
using Xunit;

namespace Nexo.Feature.Pipeline.Tests.Runtime
{
    /// <summary>
    /// Feature support tests for Unity pipeline.
    /// </summary>
    public partial class UnityPipelineTests
    {
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
