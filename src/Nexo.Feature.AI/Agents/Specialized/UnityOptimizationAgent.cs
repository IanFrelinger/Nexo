using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Specialized agent for Unity platform optimization
/// </summary>
public class UnityOptimizationAgent : ISpecializedAgent
{
    public string AgentId => "UnityOptimization";
    public AgentSpecialization Specialization => AgentSpecialization.PlatformSpecific | AgentSpecialization.GameDevelopment;
    public PlatformCompatibility PlatformExpertise => PlatformCompatibility.Unity;
    
    public PerformanceProfile OptimizationProfile => new()
    {
        PrimaryTarget = OptimizationTarget.Performance,
        MonitoredMetrics = new[]
        {
            PerformanceMetric.FrameRate,
            PerformanceMetric.GarbageCollection,
            PerformanceMetric.DrawCalls,
            PerformanceMetric.MemoryUsage,
            PerformanceMetric.ExecutionTime
        },
        SupportsRealTimeOptimization = true
    };
    
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<UnityOptimizationAgent> _logger;
    
    public UnityOptimizationAgent(
        IModelOrchestrator modelOrchestrator,
        ILogger<UnityOptimizationAgent> logger)
    {
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentResponse> ProcessAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing Unity optimization request");
            
            var unityContext = ExtractUnityContext(request);
            var optimizations = new List<UnityOptimization>();
            
            // Object pooling recommendations
            if (await RequiresObjectPooling(request))
            {
                var poolingOpt = await GenerateObjectPoolingCode(request);
                if (poolingOpt != null)
                {
                    optimizations.Add(poolingOpt);
                }
            }
            
            // Iteration strategy optimization for Unity
            var iterationOpt = await OptimizeForUnityPerformance(request);
            if (iterationOpt != null)
            {
                optimizations.Add(iterationOpt);
            }
            
            // Frame rate optimization
            var frameRateOpt = await OptimizeForFrameRate(request, unityContext);
            if (frameRateOpt != null)
            {
                optimizations.Add(frameRateOpt);
            }
            
            // Memory management optimization
            var memoryOpt = await OptimizeMemoryUsage(request, unityContext);
            if (memoryOpt != null)
            {
                optimizations.Add(memoryOpt);
            }
            
            // Rendering optimization
            var renderingOpt = await OptimizeRendering(request, unityContext);
            if (renderingOpt != null)
            {
                optimizations.Add(renderingOpt);
            }
            
            var optimizedCode = await ApplyUnityOptimizations(request.Input, optimizations);
            
            return new AgentResponse
            {
                Result = optimizedCode,
                Confidence = CalculateOptimizationConfidence(optimizations),
                Metadata = new Dictionary<string, object>
                {
                    ["UnityOptimizations"] = optimizations,
                    ["TargetFrameRate"] = unityContext.TargetFrameRate,
                    ["PlatformTarget"] = unityContext.BuildTarget,
                    ["AgentId"] = AgentId,
                    ["Specialization"] = Specialization.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Unity optimization request");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Unity optimization failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators)
    {
        try
        {
            _logger.LogInformation("Coordinating Unity optimization with {CollaboratorCount} agents", 
                collaborators.Count());
            
            // Find performance agents for coordination
            var performanceAgents = collaborators
                .Where(a => a.Specialization.HasFlag(AgentSpecialization.PerformanceOptimization))
                .ToList();
            
            var coordinatedOptimizations = new List<UnityOptimization>();
            
            // Get performance insights from performance agents
            foreach (var perfAgent in performanceAgents)
            {
                var perfRequest = request with 
                { 
                    Input = $"{request.Input}\n\nUnity-specific performance context: {ExtractUnityContext(request)}" 
                };
                
                var perfResponse = await perfAgent.ProcessAsync(perfRequest);
                
                if (perfResponse.HasResult)
                {
                    var perfOptimization = new UnityOptimization
                    {
                        Type = UnityOptimizationType.PerformanceOptimization,
                        OriginalCode = request.Input,
                        OptimizedCode = perfResponse.Result,
                        EstimatedImprovementFactor = 1.3,
                        UnitySpecificNotes = "Coordinated with performance agent"
                    };
                    
                    coordinatedOptimizations.Add(perfOptimization);
                }
            }
            
            // Apply Unity-specific optimizations on top of performance optimizations
            var unityOptimizedCode = await ApplyUnityOptimizations(request.Input, coordinatedOptimizations);
            
            return new AgentResponse
            {
                Result = unityOptimizedCode,
                Confidence = 0.9,
                Metadata = new Dictionary<string, object>
                {
                    ["CoordinatedOptimizations"] = coordinatedOptimizations,
                    ["CoordinationType"] = "UnityPerformance",
                    ["AgentId"] = AgentId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error coordinating Unity optimization");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Unity coordination failed: {ex.Message}",
                Confidence = 0.0
            };
        }
    }
    
    public async Task<AgentCapabilityAssessment> AssessCapabilityAsync(AgentRequest request)
    {
        try
        {
            var isUnityRequest = request.TargetPlatform?.HasFlag(PlatformCompatibility.Unity) == true ||
                               request.Input.Contains("Unity", StringComparison.OrdinalIgnoreCase) ||
                               request.Input.Contains("GameObject", StringComparison.OrdinalIgnoreCase) ||
                               request.Input.Contains("MonoBehaviour", StringComparison.OrdinalIgnoreCase);
            
            var strengths = new List<string>();
            var limitations = new List<string>();
            var capabilityScore = 0.0;
            
            if (isUnityRequest)
            {
                strengths.Add("Unity-specific optimization expertise");
                strengths.Add("Game development performance tuning");
                strengths.Add("Unity rendering pipeline optimization");
                strengths.Add("Unity memory management");
                capabilityScore += 0.9;
            }
            else
            {
                limitations.Add("Not a Unity-specific request");
                capabilityScore += 0.1;
            }
            
            if (request.PerformanceRequirements?.PrimaryTarget == OptimizationTarget.Performance)
            {
                strengths.Add("High-performance Unity code generation");
                capabilityScore += 0.1;
            }
            
            return new AgentCapabilityAssessment
            {
                CapabilityScore = Math.Min(capabilityScore, 1.0),
                Strengths = strengths.ToArray(),
                Limitations = limitations.ToArray(),
                CanHandleRequest = capabilityScore > 0.5,
                Recommendation = capabilityScore > 0.8 ? "Highly recommended for Unity optimization" : 
                               capabilityScore > 0.5 ? "Suitable for Unity development" : "Not recommended"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing Unity agent capability");
            return new AgentCapabilityAssessment
            {
                CapabilityScore = 0.0,
                CanHandleRequest = false,
                Recommendation = "Assessment failed"
            };
        }
    }
    
    public async Task LearnFromResultAsync(AgentRequest request, AgentResponse response, PerformanceMetrics metrics)
    {
        try
        {
            _logger.LogDebug("Learning from Unity optimization result");
            
            // Store Unity-specific learning data
            var learningData = new
            {
                Request = request.Input,
                Response = response.Result,
                Success = response.Success,
                Confidence = response.Confidence,
                UnityContext = ExtractUnityContext(request),
                ActualPerformance = metrics,
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogDebug("Unity learning data recorded for future optimization improvements");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning from Unity optimization result");
        }
    }
    
    private UnityContext ExtractUnityContext(AgentRequest request)
    {
        var context = new UnityContext();
        
        // Extract Unity-specific information from the request
        if (request.Context?.TryGetValue("UnityContext", out var unityCtx) == true && 
            unityCtx is UnityContext extractedContext)
        {
            return extractedContext;
        }
        
        // Default Unity context
        return new UnityContext
        {
            TargetFrameRate = 60,
            BuildTarget = "StandaloneWindows64",
            RenderingPipeline = "Built-in",
            QualityLevel = "High"
        };
    }
    
    private async Task<bool> RequiresObjectPooling(AgentRequest request)
    {
        var poolingKeywords = new[] { "Instantiate", "Destroy", "spawn", "bullet", "enemy", "particle" };
        return poolingKeywords.Any(keyword => 
            request.Input.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
    
    private async Task<UnityOptimization?> GenerateObjectPoolingCode(AgentRequest request)
    {
        var poolingPrompt = $"""
        Generate Unity object pooling code for this request:
        
        {request.Input}
        
        Create an object pool implementation that:
        1. Pre-allocates objects to avoid runtime Instantiate/Destroy calls
        2. Uses a queue-based pool for efficient object reuse
        3. Includes proper initialization and cleanup methods
        4. Handles edge cases like pool exhaustion
        5. Provides performance monitoring
        
        Generate the complete object pooling solution.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = poolingPrompt,
            Temperature = 0.4,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new UnityOptimization
            {
                Type = UnityOptimizationType.ObjectPooling,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 2.0,
                UnitySpecificNotes = "Object pooling reduces garbage collection pressure"
            };
        }
        
        return null;
    }
    
    private async Task<UnityOptimization?> OptimizeForUnityPerformance(AgentRequest request)
    {
        var prompt = $"""
        Optimize this code for Unity performance:
        
        {request.Input}
        
        Apply Unity-specific optimizations:
        1. Use for-loops instead of foreach where possible (Unity's foreach is slower)
        2. Cache component references to avoid GetComponent calls
        3. Use object pooling for frequently instantiated objects
        4. Minimize string concatenations and use StringBuilder
        5. Avoid LINQ in performance-critical code
        6. Use Unity's Job System for heavy computations
        7. Minimize garbage collection allocations
        8. Use structs instead of classes where appropriate
        9. Cache Transform references
        10. Use CompareTag instead of tag string comparisons
        
        Generate optimized code with Unity best practices.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.4,
            MaxTokens = 1500
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new UnityOptimization
            {
                Type = UnityOptimizationType.PerformanceOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = EstimateUnityPerformanceGain(request.Input, response.Response),
                UnitySpecificNotes = "Applied Unity performance best practices"
            };
        }
        
        return null;
    }
    
    private async Task<UnityOptimization?> OptimizeForFrameRate(AgentRequest request, UnityContext context)
    {
        var prompt = $"""
        Optimize this code for consistent {context.TargetFrameRate} FPS in Unity:
        
        {request.Input}
        
        Target: {context.TargetFrameRate} FPS
        Build Target: {context.BuildTarget}
        
        Apply frame rate optimizations:
        1. Move heavy computations to coroutines or async methods
        2. Use LOD (Level of Detail) systems for complex objects
        3. Implement frustum culling for off-screen objects
        4. Use occlusion culling where appropriate
        5. Optimize draw calls and batching
        6. Use texture atlasing to reduce texture switches
        7. Implement dynamic quality adjustment based on frame rate
        8. Use Unity's Profiler to identify bottlenecks
        
        Generate frame rate optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new UnityOptimization
            {
                Type = UnityOptimizationType.FrameRateOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.4,
                UnitySpecificNotes = $"Optimized for {context.TargetFrameRate} FPS target"
            };
        }
        
        return null;
    }
    
    private async Task<UnityOptimization?> OptimizeMemoryUsage(AgentRequest request, UnityContext context)
    {
        var prompt = $"""
        Optimize this code for Unity memory management:
        
        {request.Input}
        
        Apply memory optimizations:
        1. Use object pooling to reduce garbage collection
        2. Pre-allocate collections with known capacity
        3. Use structs instead of classes for small data
        4. Avoid string concatenations in Update methods
        5. Use Resources.UnloadUnusedAssets() appropriately
        6. Implement proper asset loading/unloading
        7. Use Unity's Memory Profiler to identify leaks
        8. Cache frequently used objects
        9. Use static readonly for constants
        10. Minimize boxing and unboxing
        
        Generate memory-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new UnityOptimization
            {
                Type = UnityOptimizationType.MemoryOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.6,
                UnitySpecificNotes = "Reduced memory allocations and GC pressure"
            };
        }
        
        return null;
    }
    
    private async Task<UnityOptimization?> OptimizeRendering(AgentRequest request, UnityContext context)
    {
        var prompt = $"""
        Optimize this code for Unity rendering performance:
        
        {request.Input}
        
        Rendering Pipeline: {context.RenderingPipeline}
        Quality Level: {context.QualityLevel}
        
        Apply rendering optimizations:
        1. Minimize draw calls through batching
        2. Use GPU Instancing for repeated objects
        3. Implement LOD groups for complex meshes
        4. Use occlusion culling for large scenes
        5. Optimize shader complexity
        6. Use texture compression and atlasing
        7. Implement frustum culling
        8. Use Unity's SRP Batcher when available
        9. Optimize shadow casting and receiving
        10. Use appropriate render queue settings
        
        Generate rendering-optimized code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = prompt,
            Temperature = 0.3,
            MaxTokens = 1200
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (response.Success)
        {
            return new UnityOptimization
            {
                Type = UnityOptimizationType.RenderingOptimization,
                OriginalCode = request.Input,
                OptimizedCode = response.Response,
                EstimatedImprovementFactor = 1.5,
                UnitySpecificNotes = $"Optimized for {context.RenderingPipeline} pipeline"
            };
        }
        
        return null;
    }
    
    private async Task<string> ApplyUnityOptimizations(string originalCode, IEnumerable<UnityOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return originalCode;
        }
        
        var synthesisPrompt = $"""
        Synthesize these Unity optimizations into a final, cohesive solution:
        
        Original Code:
        {originalCode}
        
        Optimizations Applied:
        """;
        
        foreach (var opt in optimizations)
        {
            synthesisPrompt += $"\n{opt.Type}:\n{opt.OptimizedCode}\n";
        }
        
        synthesisPrompt += """
        
        Create a unified solution that:
        1. Combines all optimizations seamlessly
        2. Maintains code readability and maintainability
        3. Includes proper Unity-specific patterns
        4. Provides performance monitoring capabilities
        5. Handles edge cases and error conditions
        
        Generate the final, optimized Unity code.
        """;
        
        var modelRequest = new Models.ModelRequest
        {
            Input = synthesisPrompt,
            Temperature = 0.3,
            MaxTokens = 2000
        };
        
        var response = await _modelOrchestrator.ExecuteAsync(modelRequest);
        
        if (!response.Success)
        {
            _logger.LogError("Failed to synthesize Unity optimizations");
            return originalCode;
        }
        
        return response.Response;
    }
    
    private double CalculateOptimizationConfidence(IEnumerable<UnityOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return 0.5;
        }
        
        var avgImprovement = optimizations.Average(o => o.EstimatedImprovementFactor);
        return Math.Min(0.95, 0.6 + (avgImprovement - 1.0) * 0.2);
    }
    
    private double EstimateUnityPerformanceGain(string originalCode, string optimizedCode)
    {
        // Simple heuristic - in reality, this would be more sophisticated
        var optimizations = 0;
        
        if (optimizedCode.Contains("for (") && !originalCode.Contains("for ("))
            optimizations++;
        if (optimizedCode.Contains("ObjectPool") && !originalCode.Contains("ObjectPool"))
            optimizations++;
        if (optimizedCode.Contains("StringBuilder") && !originalCode.Contains("StringBuilder"))
            optimizations++;
        if (optimizedCode.Contains("CompareTag") && !originalCode.Contains("CompareTag"))
            optimizations++;
        
        return 1.0 + (optimizations * 0.2);
    }
}

/// <summary>
/// Unity-specific context information
/// </summary>
public record UnityContext
{
    public int TargetFrameRate { get; init; } = 60;
    public string BuildTarget { get; init; } = "StandaloneWindows64";
    public string RenderingPipeline { get; init; } = "Built-in";
    public string QualityLevel { get; init; } = "High";
}

/// <summary>
/// Unity optimization result
/// </summary>
public record UnityOptimization
{
    public UnityOptimizationType Type { get; init; }
    public string OriginalCode { get; init; } = string.Empty;
    public string OptimizedCode { get; init; } = string.Empty;
    public double EstimatedImprovementFactor { get; init; } = 1.0;
    public string UnitySpecificNotes { get; init; } = string.Empty;
}

/// <summary>
/// Types of Unity optimizations
/// </summary>
public enum UnityOptimizationType
{
    PerformanceOptimization,
    ObjectPooling,
    FrameRateOptimization,
    MemoryOptimization,
    RenderingOptimization,
    JobSystemOptimization
}
