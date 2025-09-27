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
/// Platform analysis functionality for iteration optimization
/// </summary>
public partial class PlatformIterationAgent
{
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
}
