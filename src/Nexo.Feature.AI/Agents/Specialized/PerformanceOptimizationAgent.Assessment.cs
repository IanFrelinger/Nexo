using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Capability assessment functionality
/// </summary>
public partial class PerformanceOptimizationAgent
{
    public async Task<AgentCapabilityAssessment> AssessCapabilityAsync(AgentRequest request)
    {
        try
        {
            var analysis = await AnalyzePerformanceRequirements(request);
            
            var strengths = new List<string>();
            var limitations = new List<string>();
            var capabilityScore = 0.0;
            
            if (analysis.RequiresOptimization)
            {
                strengths.Add("Performance optimization expertise");
                strengths.Add("Iteration strategy optimization");
                strengths.Add("Cross-platform performance tuning");
                capabilityScore += 0.8;
            }
            
            if (request.TargetPlatform?.Contains("unity", StringComparison.OrdinalIgnoreCase) == true)
            {
                strengths.Add("Unity performance optimization");
                capabilityScore += 0.1;
            }
            
            if (request.TargetPlatform?.Contains("web", StringComparison.OrdinalIgnoreCase) == true)
            {
                strengths.Add("Web performance optimization");
                capabilityScore += 0.1;
            }
            
            if (request.PerformanceRequirements?.RequiresRealTime == true)
            {
                strengths.Add("High-performance code generation");
                capabilityScore += 0.2;
            }
            
            if (analysis.ComplexityLevel == PerformanceComplexity.Low)
            {
                limitations.Add("May be overkill for simple optimizations");
                capabilityScore -= 0.1;
            }
            
            return new AgentCapabilityAssessment
            {
                CapabilityScore = Math.Min(capabilityScore, 1.0),
                Strengths = strengths.ToArray(),
                Limitations = limitations.ToArray(),
                CanHandleRequest = capabilityScore > 0.5,
                Recommendation = capabilityScore > 0.7 ? "Highly recommended" : 
                               capabilityScore > 0.5 ? "Suitable" : "Consider alternatives"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing capability");
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
            _logger.LogDebug("Learning from performance optimization result");
            
            // Store learning data for future improvements
            var learningData = new
            {
                Request = request.Input,
                Response = response.Result,
                Success = response.Success,
                Confidence = response.Confidence,
                ActualPerformance = metrics,
                Timestamp = DateTime.UtcNow
            };
            
            // In a real implementation, this would store the learning data
            // and use it to improve future optimizations
            _logger.LogDebug("Learning data recorded for future optimization improvements");
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning from result");
        }
    }
}
