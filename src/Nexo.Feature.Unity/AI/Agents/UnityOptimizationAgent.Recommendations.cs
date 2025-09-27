using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// Recommendation functionality for Unity optimization agent
    /// </summary>
    public partial class UnityOptimizationAgent
    {
        private async Task<IEnumerable<UnityOptimizationRecommendation>> GenerateOptimizationRecommendations(
            UnityProjectAnalysis projectAnalysis, 
            UnityOptimizationRequest request)
        {
            var recommendations = new List<UnityOptimizationRecommendation>();
            
            // Generate AI-powered recommendations based on project analysis
            var prompt = $"""
            Analyze this Unity project and provide optimization recommendations:
            
            Project Analysis:
            - Scripts: {projectAnalysis.ScriptAnalysis.Scripts.Count()}
            - Scenes: {projectAnalysis.SceneAnalysis.Scenes.Count()}
            - Assets: {projectAnalysis.AssetAnalysis.Assets.Count()}
            - Performance Issues: {projectAnalysis.ScriptAnalysis.PerformanceIssues.Count()}
            - Iteration Optimizations: {projectAnalysis.IterationOptimizations.Count()}
            
            Target Platform: {request.TargetPlatform}
            Performance Goals: {request.PerformanceGoals}
            Optimization Focus: {request.OptimizationFocus}
            
            Provide specific Unity optimization recommendations:
            1. Script performance optimizations
            2. Rendering optimizations
            3. Memory optimizations
            4. Build optimizations
            5. Platform-specific optimizations
            
            Include:
            - Specific code changes
            - Unity settings adjustments
            - Asset optimizations
            - Performance impact estimates
            - Implementation difficulty
            
            Focus on practical, actionable optimizations for Unity development.
            """;
            
            var modelRequest = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 1200,
                Temperature = 0.6
            };
            
            var response = await _modelOrchestrator.ProcessAsync(modelRequest);
            return ParseOptimizationRecommendations(response.Response, projectAnalysis);
        }

        private IEnumerable<UnityOptimizationRecommendation> ParseOptimizationRecommendations(string aiResponse, UnityProjectAnalysis projectAnalysis)
        {
            var recommendations = new List<UnityOptimizationRecommendation>();
            
            try
            {
                var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") || 
                        line.StartsWith("4.") || line.StartsWith("5."))
                    {
                        var recommendation = ParseRecommendationLine(line);
                        if (recommendation != null)
                        {
                            recommendations.Add(recommendation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse optimization recommendations from AI response");
            }
            
            return recommendations;
        }

        private UnityOptimizationRecommendation? ParseRecommendationLine(string line)
        {
            var lowerLine = line.ToLower();
            
            var type = DetermineOptimizationType(lowerLine);
            var priority = DeterminePriority(lowerLine);
            
            return new UnityOptimizationRecommendation
            {
                Type = type,
                Priority = priority,
                Description = line.Substring(2).Trim(),
                SpecificActions = ExtractSpecificActions(line),
                EstimatedImprovement = EstimateImprovement(lowerLine),
                TargetPlatforms = new[] { UnityBuildTarget.Android, UnityBuildTarget.iOS }
            };
        }

        private UnityOptimizationType DetermineOptimizationType(string line)
        {
            if (line.Contains("script") || line.Contains("code") || line.Contains("performance"))
                return UnityOptimizationType.FrameRate;
            
            if (line.Contains("memory") || line.Contains("allocation") || line.Contains("gc"))
                return UnityOptimizationType.Memory;
            
            if (line.Contains("rendering") || line.Contains("draw") || line.Contains("gpu"))
                return UnityOptimizationType.Rendering;
            
            if (line.Contains("build") || line.Contains("size") || line.Contains("compression"))
                return UnityOptimizationType.BuildSize;
            
            return UnityOptimizationType.FrameRate;
        }

        private OptimizationPriority DeterminePriority(string line)
        {
            if (line.Contains("critical") || line.Contains("urgent") || line.Contains("high"))
                return OptimizationPriority.High;
            
            if (line.Contains("medium") || line.Contains("moderate"))
                return OptimizationPriority.Medium;
            
            return OptimizationPriority.Low;
        }

        private List<string> ExtractSpecificActions(string line)
        {
            var actions = new List<string>();
            
            if (line.Contains("foreach"))
                actions.Add("Replace foreach with for loops");
            
            if (line.Contains("getcomponent"))
                actions.Add("Cache GetComponent calls");
            
            if (line.Contains("instantiate"))
                actions.Add("Implement object pooling");
            
            if (line.Contains("texture"))
                actions.Add("Optimize texture sizes and formats");
            
            return actions;
        }

        private double EstimateImprovement(string line)
        {
            if (line.Contains("significant") || line.Contains("major"))
                return 0.4; // 40% improvement
            
            if (line.Contains("moderate") || line.Contains("noticeable"))
                return 0.2; // 20% improvement
            
            return 0.1; // 10% improvement
        }

        private Dictionary<string, double> CalculateEstimatedImprovements(IEnumerable<UnityOptimizationRecommendation> recommendations)
        {
            var improvements = new Dictionary<string, double>();
            
            foreach (var rec in recommendations)
            {
                var key = rec.Type.ToString();
                if (improvements.ContainsKey(key))
                {
                    improvements[key] += rec.EstimatedImprovement;
                }
                else
                {
                    improvements[key] = rec.EstimatedImprovement;
                }
            }
            
            return improvements;
        }
    }
}
