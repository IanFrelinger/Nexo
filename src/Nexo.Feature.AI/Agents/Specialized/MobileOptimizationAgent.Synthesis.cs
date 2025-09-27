using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Synthesis and utility methods for MobileOptimizationAgent.
/// </summary>
public partial class MobileOptimizationAgent
{
    private async Task<string> ApplyMobileOptimizations(string originalCode, IEnumerable<MobileOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return originalCode;
        }
        
        var synthesisPrompt = $"""
        Synthesize these mobile optimizations into a final, cohesive solution:
        
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
        2. Maintains mobile best practices and guidelines
        3. Includes proper error handling and monitoring
        4. Provides performance monitoring capabilities
        5. Handles cross-platform mobile compatibility
        
        Generate the final, optimized mobile code.
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
            _logger.LogError("Failed to synthesize mobile optimizations");
            return originalCode;
        }
        
        return response.Response;
    }
    
    private double CalculateOptimizationConfidence(IEnumerable<MobileOptimization> optimizations)
    {
        if (!optimizations.Any())
        {
            return 0.5;
        }
        
        var avgImprovement = optimizations.Average(o => o.EstimatedImprovementFactor);
        return Math.Min(0.95, 0.6 + (avgImprovement - 1.0) * 0.2);
    }
}
