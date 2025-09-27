using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// JSON response parsing functionality for different analysis types
    /// </summary>
    public partial class DecisionEngine
    {
        private ComplexityAnalysis ParseComplexityAnalysis(string jsonResponse)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;
                
                var overallComplexity = root.GetProperty("overallComplexity").GetDouble();
                var domainComplexity = root.GetProperty("domainComplexity").GetDouble();
                var technicalComplexity = root.GetProperty("technicalComplexity").GetDouble();
                var integrationComplexity = root.GetProperty("integrationComplexity").GetDouble();
                
                var factors = new List<ComplexityFactor>();
                if (root.TryGetProperty("factors", out var factorsArray))
                {
                    foreach (var factorElement in factorsArray.EnumerateArray())
                    {
                        var name = factorElement.GetProperty("name").GetString() ?? "Unknown";
                        var score = factorElement.GetProperty("score").GetDouble();
                        var description = factorElement.GetProperty("description").GetString() ?? "No description";
                        
                        factors.Add(new ComplexityFactor(name, score, description));
                    }
                }
                
                return new ComplexityAnalysis(overallComplexity, domainComplexity, technicalComplexity, integrationComplexity, factors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse complexity analysis from AI response");
                throw;
            }
        }

        private PerformanceAnalysis ParsePerformanceAnalysis(string jsonResponse)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;
                
                var levelText = root.GetProperty("level").GetString();
                var level = Enum.TryParse<PerformanceLevel>(levelText, true, out var parsedLevel) ? parsedLevel : PerformanceLevel.Medium;
                
                var throughputElement = root.GetProperty("throughput");
                var throughput = new ThroughputRequirements(
                    throughputElement.GetProperty("requestsPerSecond").GetInt32(),
                    throughputElement.GetProperty("description").GetString() ?? "No description"
                );
                
                var latencyElement = root.GetProperty("latency");
                var latency = new LatencyRequirements(
                    TimeSpan.Parse(latencyElement.GetProperty("maxLatency").GetString() ?? "00:00:01"),
                    latencyElement.GetProperty("description").GetString() ?? "No description"
                );
                
                var scalabilityElement = root.GetProperty("scalability");
                var scalability = new ScalabilityRequirements(
                    scalabilityElement.GetProperty("maxConcurrentUsers").GetInt32(),
                    scalabilityElement.GetProperty("description").GetString() ?? "No description"
                );
                
                return new PerformanceAnalysis(level, throughput, latency, scalability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse performance analysis from AI response");
                throw;
            }
        }

        private PlatformOptimizationRecommendation ParsePlatformOptimizationRecommendation(string jsonResponse)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;
                
                var platformText = root.GetProperty("platform").GetString();
                var platform = Enum.TryParse<TargetPlatform>(platformText, true, out var parsedPlatform) ? parsedPlatform : TargetPlatform.DotNet;
                
                var recommendations = new List<OptimizationRecommendation>();
                if (root.TryGetProperty("recommendations", out var recommendationsArray))
                {
                    foreach (var recElement in recommendationsArray.EnumerateArray())
                    {
                        var typeText = recElement.GetProperty("type").GetString();
                        var type = Enum.TryParse<OptimizationType>(typeText, true, out var parsedType) ? parsedType : OptimizationType.Performance;
                        var description = recElement.GetProperty("description").GetString() ?? "No description";
                        var impact = recElement.GetProperty("impact").GetString() ?? "No impact";
                        
                        recommendations.Add(new OptimizationRecommendation(type, description, impact));
                    }
                }
                
                return new PlatformOptimizationRecommendation(platform, recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse platform optimization recommendation from AI response");
                throw;
            }
        }
    }
}
