using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// Decision logic functionality for strategy determination and confidence calculation
    /// </summary>
    public partial class DecisionEngine
    {
        private ExecutionStrategy DetermineStrategyFromAnalysis(
            ComplexityAnalysis complexityAnalysis,
            PerformanceAnalysis performanceAnalysis,
            PlatformOptimizationRecommendation platformOptimizations)
        {
            // Simple decision logic - can be enhanced with more sophisticated rules
            if (complexityAnalysis.OverallComplexity > 0.8 || performanceAnalysis.Level == PerformanceLevel.Critical)
            {
                return ExecutionStrategy.Hybrid;
            }
            else if (complexityAnalysis.OverallComplexity < 0.3 && performanceAnalysis.Level == PerformanceLevel.Low)
            {
                return ExecutionStrategy.Runtime;
            }
            else
            {
                return ExecutionStrategy.Generated;
            }
        }

        private double CalculateConfidence(
            ComplexityAnalysis complexityAnalysis,
            PerformanceAnalysis performanceAnalysis,
            PlatformOptimizationRecommendation platformOptimizations)
        {
            // Simple confidence calculation - can be enhanced
            var baseConfidence = 0.8;
            var complexityFactor = 1.0 - Math.Abs(complexityAnalysis.OverallComplexity - 0.5) * 2;
            var performanceFactor = performanceAnalysis.Level == PerformanceLevel.Medium ? 1.0 : 0.8;
            
            return Math.Min(1.0, baseConfidence * complexityFactor * performanceFactor);
        }

        private string GenerateReasoning(
            ComplexityAnalysis complexityAnalysis,
            PerformanceAnalysis performanceAnalysis,
            PlatformOptimizationRecommendation platformOptimizations,
            ExecutionStrategy strategy)
        {
            var reasons = new List<string>();
            
            if (complexityAnalysis.OverallComplexity > 0.7)
            {
                reasons.Add($"High complexity ({complexityAnalysis.OverallComplexity:F2}) requires hybrid approach");
            }
            
            if (performanceAnalysis.Level == PerformanceLevel.Critical)
            {
                reasons.Add($"Critical performance requirements favor hybrid strategy");
            }
            
            if (complexityAnalysis.OverallComplexity < 0.3)
            {
                reasons.Add($"Low complexity ({complexityAnalysis.OverallComplexity:F2}) allows runtime approach");
            }
            
            if (reasons.Count == 0)
            {
                reasons.Add("Standard complexity and performance requirements favor generated approach");
            }
            
            return string.Join("; ", reasons);
        }

        private List<DecisionFactor> GenerateDecisionFactors(
            ComplexityAnalysis complexityAnalysis,
            PerformanceAnalysis performanceAnalysis,
            PlatformOptimizationRecommendation platformOptimizations)
        {
            var factors = new List<DecisionFactor>();
            
            factors.Add(new DecisionFactor(
                "Complexity",
                complexityAnalysis.OverallComplexity,
                complexityAnalysis.OverallComplexity > 0.7 ? FactorImpact.Negative : FactorImpact.Positive,
                $"Overall complexity score: {complexityAnalysis.OverallComplexity:F2}"
            ));
            
            factors.Add(new DecisionFactor(
                "Performance",
                (double)performanceAnalysis.Level / 3.0, // Convert enum to 0-1 scale
                performanceAnalysis.Level == PerformanceLevel.Critical ? FactorImpact.Negative : FactorImpact.Positive,
                $"Performance level: {performanceAnalysis.Level}"
            ));
            
            factors.Add(new DecisionFactor(
                "Platform",
                0.5, // Neutral for now
                FactorImpact.Neutral,
                $"Target platform: {platformOptimizations.Platform}"
            ));
            
            return factors;
        }
    }
}
