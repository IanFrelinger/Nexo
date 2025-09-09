using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Agents;

/// <summary>
/// AI agent specialized in platform-specific iteration optimizations
/// </summary>
public class PlatformIterationAgent : IAIAgent
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<PlatformIterationAgent> _logger;
    
    public string AgentId => "PlatformIteration";
    public AgentCapabilities Capabilities => AgentCapabilities.CodeGeneration | AgentCapabilities.PlatformOptimization;
    
    public PlatformIterationAgent(
        IIterationStrategySelector strategySelector,
        IModelOrchestrator modelOrchestrator,
        ILogger<PlatformIterationAgent> logger)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AgentResponse> ProcessAsync(AgentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing platform-specific iteration optimization request");
            
            // Analyze the platform-specific requirements
            var platformAnalysis = await AnalyzePlatformRequirements(request.Input);
            
            if (platformAnalysis.RequiresPlatformOptimization)
            {
                // Get platform-specific strategy recommendations
                var recommendations = _strategySelector.GetRecommendations(ConvertToPlatformTarget(platformAnalysis.PlatformType));
                
                // Select the best strategy for this platform
                var strategy = _strategySelector.SelectStrategy<object>(platformAnalysis.IterationContext);
                
                // Generate platform-optimized code
                var optimizedCode = await GeneratePlatformOptimizedCode(platformAnalysis, strategy);
                
                _logger.LogInformation("Generated platform-optimized iteration code for {Platform}", platformAnalysis.PlatformType);
                
                return new AgentResponse
                {
                    Result = optimizedCode,
                    Confidence = CalculatePlatformConfidence(platformAnalysis, strategy),
                    Metadata = new Dictionary<string, object>
                    {
                        ["PlatformType"] = platformAnalysis.PlatformType,
                        ["IterationStrategy"] = strategy.StrategyId,
                        ["PlatformOptimizations"] = platformAnalysis.PlatformOptimizations,
                        ["Recommendations"] = recommendations,
                        ["PerformanceEstimate"] = strategy.EstimatePerformance(platformAnalysis.IterationContext)
                    }
                };
            }
            
            return AgentResponse.NoAction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing platform iteration optimization request");
            return new AgentResponse
            {
                Result = string.Empty,
                Confidence = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }
    
    private async Task<PlatformAnalysis> AnalyzePlatformRequirements(string input)
    {
        var analysisPrompt = $"""
        Analyze this code generation request for platform-specific iteration requirements:
        
        {input}
        
        Determine:
        1. What platform is being targeted?
        2. What are the platform-specific constraints?
        3. What iteration patterns are most suitable for this platform?
        4. What performance characteristics are important?
        5. Are there any platform-specific APIs or features to use?
        6. What are the memory and CPU constraints?
        7. Are there any platform-specific best practices to follow?
        
        Respond with structured analysis in the following format:
        - PlatformType: platform name
        - RequiresPlatformOptimization: true/false
        - PlatformConstraints: constraints
        - PerformanceCharacteristics: characteristics
        - PlatformSpecificAPIs: APIs to use
        - MemoryConstraints: memory limits
        - CPUConstraints: CPU limits
        - BestPractices: best practices
        """;
        
        var response = await _modelOrchestrator.ProcessAsync(new ModelRequest { Input = analysisPrompt });
        return ParsePlatformAnalysis(response.Response);
    }
    
    private PlatformAnalysis ParsePlatformAnalysis(string response)
    {
        var analysis = new PlatformAnalysis
        {
            PlatformType = ExtractPlatformType(response),
            RequiresPlatformOptimization = response.Contains("RequiresPlatformOptimization: true"),
            PlatformConstraints = ExtractString(response, "PlatformConstraints") ?? string.Empty,
            PerformanceCharacteristics = ExtractString(response, "PerformanceCharacteristics") ?? string.Empty,
            PlatformSpecificAPIs = ExtractString(response, "PlatformSpecificAPIs") ?? string.Empty,
            MemoryConstraints = ExtractString(response, "MemoryConstraints") ?? string.Empty,
            CPUConstraints = ExtractString(response, "CPUConstraints") ?? string.Empty,
            BestPractices = ExtractString(response, "BestPractices") ?? string.Empty,
            PlatformOptimizations = new List<string>()
        };
        
        // Create iteration context based on platform analysis
        var iterationContext = new IterationContext
        {
            DataSize = EstimateDataSizeFromAnalysis(response),
            Requirements = CreatePerformanceRequirementsFromAnalysis(response).ToIterationRequirements(),
            EnvironmentProfile = CreateEnvironmentProfileFromAnalysis(analysis.PlatformType),
            TargetPlatform = GetPlatformTargetFromType(analysis.PlatformType),
            IsCpuBound = IsCpuBoundFromAnalysis(response),
            IsIoBound = IsIoBoundFromAnalysis(response),
            RequiresAsync = RequiresAsyncFromAnalysis(response)
        };
        
        analysis = analysis with { IterationContext = iterationContext };
        
        return analysis;
    }
    
    private PlatformType ExtractPlatformType(string response)
    {
        if (response.Contains("Unity"))
            return PlatformType.Unity;
        if (response.Contains("JavaScript") || response.Contains("Web"))
            return PlatformType.JavaScript;
        if (response.Contains("Swift") || response.Contains("iOS"))
            return PlatformType.Swift;
        if (response.Contains("Kotlin") || response.Contains("Android"))
            return PlatformType.Kotlin;
        if (response.Contains("WebAssembly"))
            return PlatformType.WebAssembly;
        if (response.Contains("Mobile"))
            return PlatformType.Mobile;
        if (response.Contains("Server"))
            return PlatformType.Server;
        
        return PlatformType.DotNet;
    }
    
    private string? ExtractString(string text, string key)
    {
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains(key + ":"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }
        }
        return null;
    }
    
    private int EstimateDataSizeFromAnalysis(string response)
    {
        if (response.Contains("small") || response.Contains("Small"))
            return 100;
        if (response.Contains("medium") || response.Contains("Medium"))
            return 1000;
        if (response.Contains("large") || response.Contains("Large"))
            return 10000;
        if (response.Contains("huge") || response.Contains("Huge"))
            return 100000;
        
        return 1000; // Default
    }
    
    private PerformanceRequirements CreatePerformanceRequirementsFromAnalysis(string response)
    {
        var requirements = new PerformanceRequirements();
        
        if (response.Contains("real-time") || response.Contains("Real-time"))
        {
            requirements = requirements with { RequiresRealTime = true };
        }
        
        if (response.Contains("parallel") || response.Contains("Parallel"))
        {
            requirements = requirements with { PreferParallel = true };
        }
        
        if (response.Contains("memory") || response.Contains("Memory"))
        {
            requirements = requirements with { MemoryCritical = true };
        }
        
        return requirements;
    }
    
    private RuntimeEnvironmentProfile CreateEnvironmentProfileFromAnalysis(PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Unity => new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Unity,
                CpuCores = 4,
                AvailableMemoryMB = 1024,
                IsConstrained = true,
                IsMobile = false,
                IsWeb = false,
                IsUnity = true
            },
            PlatformType.Mobile => new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Mobile,
                CpuCores = 2,
                AvailableMemoryMB = 512,
                IsConstrained = true,
                IsMobile = true,
                IsWeb = false,
                IsUnity = false
            },
            PlatformType.Server => new RuntimeEnvironmentProfile
            {
                PlatformType = PlatformType.Server,
                CpuCores = 8,
                AvailableMemoryMB = 8192,
                IsConstrained = false,
                IsMobile = false,
                IsWeb = false,
                IsUnity = false
            },
            _ => RuntimeEnvironmentDetector.DetectCurrent()
        };
    }
    
    private PlatformTarget GetPlatformTargetFromType(PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Unity => PlatformTarget.Unity2023,
            PlatformType.JavaScript => PlatformTarget.JavaScript,
            PlatformType.Swift => PlatformTarget.Swift,
            PlatformType.Kotlin => PlatformTarget.Kotlin,
            PlatformType.WebAssembly => PlatformTarget.WebAssembly,
            PlatformType.Server => PlatformTarget.Server,
            _ => PlatformTarget.DotNet
        };
    }
    
    private bool IsCpuBoundFromAnalysis(string response)
    {
        return response.Contains("CPU") || response.Contains("compute") || response.Contains("calculation");
    }
    
    private bool IsIoBoundFromAnalysis(string response)
    {
        return response.Contains("I/O") || response.Contains("network") || response.Contains("file");
    }
    
    private bool RequiresAsyncFromAnalysis(string response)
    {
        return response.Contains("async") || response.Contains("await") || response.Contains("concurrent");
    }
    
    private async Task<string> GeneratePlatformOptimizedCode(PlatformAnalysis analysis, IIterationStrategy<object> strategy)
    {
        var codeGenerationContext = new CodeGenerationContext
        {
            PlatformTarget = GetPlatformTargetFromType(analysis.PlatformType),
            CollectionVariableName = "items",
            ItemVariableName = "item",
            ActionCode = "// Process item",
            IncludeNullChecks = true,
            IncludeBoundsChecking = true,
            PerformanceRequirements = analysis.IterationContext.Requirements.ToPerformanceRequirements(),
            AdditionalContext = new Dictionary<string, object>
            {
                ["PlatformConstraints"] = analysis.PlatformConstraints,
                ["PerformanceCharacteristics"] = analysis.PerformanceCharacteristics,
                ["PlatformSpecificAPIs"] = analysis.PlatformSpecificAPIs,
                ["BestPractices"] = analysis.BestPractices
            }
        };
        
        var baseCode = strategy.GenerateCode(codeGenerationContext);
        
        // Enhance with platform-specific optimizations
        var enhancementPrompt = CreatePlatformEnhancementPrompt(baseCode, analysis);
        var enhanced = await _modelOrchestrator.ProcessAsync(new ModelRequest { Input = enhancementPrompt });
        
        return enhanced.Response;
    }
    
    private string CreatePlatformEnhancementPrompt(string baseCode, PlatformAnalysis analysis)
    {
        return $"""
        Enhance this iteration code with platform-specific optimizations for {analysis.PlatformType}:
        
        Base Code:
        {baseCode}
        
        Platform Analysis:
        - Platform Type: {analysis.PlatformType}
        - Constraints: {analysis.PlatformConstraints}
        - Performance Characteristics: {analysis.PerformanceCharacteristics}
        - Platform-Specific APIs: {analysis.PlatformSpecificAPIs}
        - Memory Constraints: {analysis.MemoryConstraints}
        - CPU Constraints: {analysis.CPUConstraints}
        - Best Practices: {analysis.BestPractices}
        
        Please enhance the code with:
        1. Platform-specific optimizations
        2. Memory management best practices
        3. Performance optimizations for the target platform
        4. Platform-specific APIs and features
        5. Error handling appropriate for the platform
        6. Platform-specific best practices
        
        Return only the enhanced code without explanations.
        """;
    }
    
    private double CalculatePlatformConfidence(PlatformAnalysis analysis, IIterationStrategy<object> strategy)
    {
        var confidence = 0.8; // Base confidence
        
        // Increase confidence based on platform-specific knowledge
        if (!string.IsNullOrEmpty(analysis.PlatformConstraints))
        {
            confidence += 0.1;
        }
        
        if (!string.IsNullOrEmpty(analysis.BestPractices))
        {
            confidence += 0.1;
        }
        
        // Increase confidence for well-defined platform requirements
        if (analysis.RequiresPlatformOptimization)
        {
            confidence += 0.05;
        }
        
        return Math.Min(1.0, confidence);
    }
    
    private static PlatformTarget ConvertToPlatformTarget(Nexo.Core.Domain.Entities.Infrastructure.PlatformType platformType)
    {
        return platformType switch
        {
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.DotNet => PlatformTarget.DotNet,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Unity => PlatformTarget.Unity,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly => PlatformTarget.WebAssembly,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile => PlatformTarget.Mobile,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Server => PlatformTarget.Server,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Browser => PlatformTarget.Browser,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Native => PlatformTarget.Native,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows => PlatformTarget.Windows,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux => PlatformTarget.Linux,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS => PlatformTarget.macOS,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web => PlatformTarget.Web,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.JavaScript => PlatformTarget.JavaScript,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Swift => PlatformTarget.Swift,
            Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Kotlin => PlatformTarget.Kotlin,
            _ => PlatformTarget.DotNet
        };
    }
}

/// <summary>
/// Analysis of platform-specific requirements
/// </summary>
public record PlatformAnalysis
{
    /// <summary>
    /// Platform type
    /// </summary>
    public PlatformType PlatformType { get; init; }
    
    /// <summary>
    /// Whether platform optimization is required
    /// </summary>
    public bool RequiresPlatformOptimization { get; init; }
    
    /// <summary>
    /// Platform constraints
    /// </summary>
    public string PlatformConstraints { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance characteristics
    /// </summary>
    public string PerformanceCharacteristics { get; init; } = string.Empty;
    
    /// <summary>
    /// Platform-specific APIs
    /// </summary>
    public string PlatformSpecificAPIs { get; init; } = string.Empty;
    
    /// <summary>
    /// Memory constraints
    /// </summary>
    public string MemoryConstraints { get; init; } = string.Empty;
    
    /// <summary>
    /// CPU constraints
    /// </summary>
    public string CPUConstraints { get; init; } = string.Empty;
    
    /// <summary>
    /// Best practices
    /// </summary>
    public string BestPractices { get; init; } = string.Empty;
    
    /// <summary>
    /// Platform optimizations
    /// </summary>
    public List<string> PlatformOptimizations { get; init; } = new();
    
    /// <summary>
    /// Iteration context
    /// </summary>
    public IterationContext? IterationContext { get; init; }
}
