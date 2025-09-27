using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Synthesis and utility methods for WebOptimizationAgent.
/// </summary>
public partial class WebOptimizationAgent
{
    private async Task<string> ApplyWebOptimizations(string originalCode, IEnumerable<WebOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return originalCode;
        }
        
        var synthesisPrompt = $"""
        Synthesize these web optimizations into a final, cohesive solution:
        
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
        2. Maintains web standards and best practices
        3. Includes proper error handling and monitoring
        4. Provides performance monitoring capabilities
        5. Handles cross-browser compatibility
        
        Generate the final, optimized web code.
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
            _logger.LogError("Failed to synthesize web optimizations");
            return originalCode;
        }
        
        return response.Response;
    }
    
    private double CalculateOptimizationConfidence(IEnumerable<WebOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return 0.5;
        }
        
        var avgImprovement = optimizations.Average(o => o.EstimatedImprovementFactor);
        return Math.Min(0.95, 0.6 + (avgImprovement - 1.0) * 0.2);
    }
}
