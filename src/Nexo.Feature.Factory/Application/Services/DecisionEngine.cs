using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Feature.Factory.Domain.Enums;

namespace Nexo.Feature.Factory.Application.Services
{
    /// <summary>
    /// AI-powered decision engine that analyzes feature requirements and chooses the optimal execution strategy.
    /// </summary>
    public sealed class DecisionEngine : IDecisionEngine
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<DecisionEngine> _logger;

        public DecisionEngine(IModelOrchestrator modelOrchestrator, ILogger<DecisionEngine> logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExecutionStrategyDecision> DetermineStrategyAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Determining execution strategy for specification: {SpecificationId}", specification.Id);

            try
            {
                // Analyze complexity
                var complexityAnalysis = await AnalyzeComplexityAsync(specification, cancellationToken);
                
                // Analyze performance requirements
                var performanceAnalysis = await AnalyzePerformanceRequirementsAsync(specification, cancellationToken);
                
                // Analyze platform optimizations
                var platformOptimizations = await AnalyzePlatformOptimizationsAsync(specification, cancellationToken);

                // Determine strategy based on analysis
                var strategy = DetermineStrategyFromAnalysis(complexityAnalysis, performanceAnalysis, platformOptimizations);
                var confidence = CalculateConfidence(complexityAnalysis, performanceAnalysis, platformOptimizations);
                var reasoning = GenerateReasoning(complexityAnalysis, performanceAnalysis, platformOptimizations, strategy);
                var factors = GenerateDecisionFactors(complexityAnalysis, performanceAnalysis, platformOptimizations);

                var decision = new ExecutionStrategyDecision(strategy, confidence, reasoning, factors);
                
                _logger.LogInformation("Execution strategy determined: {Strategy} (confidence: {Confidence})", strategy, confidence);
                
                return decision;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining execution strategy for specification: {SpecificationId}", specification.Id);
                
                // Fallback to generated strategy
                return new ExecutionStrategyDecision(
                    ExecutionStrategy.Generated,
                    0.5,
                    "Fallback to generated strategy due to analysis error",
                    new List<DecisionFactor>
                    {
                        new DecisionFactor("Error", 1.0, FactorImpact.Negative, "Analysis failed, using safe fallback")
                    }
                );
            }
        }

        public async Task<ComplexityAnalysis> AnalyzeComplexityAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing complexity for specification: {SpecificationId}", specification.Id);

            try
            {
                var prompt = $@"
Analyze the complexity of the following feature specification and provide scores (0.0 to 1.0) for different complexity dimensions:

Feature Description: {specification.Description}
Target Platform: {specification.TargetPlatform}
Number of Entities: {specification.Entities.Count}
Number of Value Objects: {specification.ValueObjects.Count}
Number of Business Rules: {specification.BusinessRules.Count}
Number of Validation Rules: {specification.ValidationRules.Count}

Entities:
{string.Join("\n", specification.Entities.Select(e => $"- {e.Name}: {e.Description} ({e.Properties.Count} properties)"))}

Value Objects:
{string.Join("\n", specification.ValueObjects.Select(vo => $"- {vo.Name}: {vo.Description} ({vo.Properties.Count} properties)"))}

Business Rules:
{string.Join("\n", specification.BusinessRules.Select(br => $"- {br.Name}: {br.Description}"))}

Return a JSON response with the following structure:
{{
  ""overallComplexity"": 0.5,
  ""domainComplexity"": 0.4,
  ""technicalComplexity"": 0.6,
  ""integrationComplexity"": 0.3,
  ""factors"": [
    {{
      ""name"": ""FactorName"",
      ""score"": 0.5,
      ""description"": ""Factor description""
    }}
  ]
}}

Consider these factors:
1. Domain complexity: Business logic complexity, number of entities, relationships
2. Technical complexity: Technical requirements, platform-specific needs
3. Integration complexity: External dependencies, API integrations
4. Overall complexity: Weighted combination of all factors

Return only valid JSON:";

                var response = await CallAIAsync(prompt, cancellationToken);
                return ParseComplexityAnalysis(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing complexity for specification: {SpecificationId}", specification.Id);
                
                // Return default complexity analysis
                return new ComplexityAnalysis(
                    0.5, // Overall complexity
                    0.5, // Domain complexity
                    0.5, // Technical complexity
                    0.5, // Integration complexity
                    new List<ComplexityFactor>
                    {
                        new ComplexityFactor("Default", 0.5, "Default complexity due to analysis error")
                    }
                );
            }
        }

        public async Task<PerformanceAnalysis> AnalyzePerformanceRequirementsAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing performance requirements for specification: {SpecificationId}", specification.Id);

            try
            {
                var prompt = $@"
Analyze the performance requirements for the following feature specification:

Feature Description: {specification.Description}
Target Platform: {specification.TargetPlatform}
Number of Entities: {specification.Entities.Count}
Number of Business Rules: {specification.BusinessRules.Count}

Entities:
{string.Join("\n", specification.Entities.Select(e => $"- {e.Name}: {e.Description}"))}

Business Rules:
{string.Join("\n", specification.BusinessRules.Select(br => $"- {br.Name}: {br.Description}"))}

Return a JSON response with the following structure:
{{
  ""level"": ""Medium"",
  ""throughput"": {{
    ""requestsPerSecond"": 100,
    ""description"": ""Expected throughput""
  }},
  ""latency"": {{
    ""maxLatency"": ""00:00:01"",
    ""description"": ""Maximum acceptable latency""
  }},
  ""scalability"": {{
    ""maxConcurrentUsers"": 1000,
    ""description"": ""Maximum concurrent users""
  }}
}}

Consider these factors:
1. Data volume and processing requirements
2. Real-time vs batch processing needs
3. User concurrency expectations
4. Platform-specific performance characteristics
5. Business criticality

Return only valid JSON:";

                var response = await CallAIAsync(prompt, cancellationToken);
                return ParsePerformanceAnalysis(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing performance requirements for specification: {SpecificationId}", specification.Id);
                
                // Return default performance analysis
                return new PerformanceAnalysis(
                    PerformanceLevel.Medium,
                    new ThroughputRequirements(100, "Default throughput requirements"),
                    new LatencyRequirements(TimeSpan.FromSeconds(1), "Default latency requirements"),
                    new ScalabilityRequirements(1000, "Default scalability requirements")
                );
            }
        }

        public async Task<PlatformOptimizationRecommendation> AnalyzePlatformOptimizationsAsync(FeatureSpecification specification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Analyzing platform optimizations for specification: {SpecificationId}", specification.Id);

            try
            {
                var prompt = $@"
Analyze platform-specific optimizations for the following feature specification:

Feature Description: {specification.Description}
Target Platform: {specification.TargetPlatform}
Number of Entities: {specification.Entities.Count}

Entities:
{string.Join("\n", specification.Entities.Select(e => $"- {e.Name}: {e.Description}"))}

Return a JSON response with the following structure:
{{
  ""platform"": ""{specification.TargetPlatform}"",
  ""recommendations"": [
    {{
      ""type"": ""Performance"",
      ""description"": ""Optimization description"",
      ""impact"": ""Expected impact""
    }}
  ]
}}

Consider these optimization types:
1. Performance: CPU, memory, I/O optimizations
2. Memory: Memory usage and garbage collection
3. Network: Network communication and bandwidth
4. Caching: Caching strategies and invalidation
5. Database: Database queries and indexing
6. Security: Security best practices and vulnerabilities

Return only valid JSON:";

                var response = await CallAIAsync(prompt, cancellationToken);
                return ParsePlatformOptimizationRecommendation(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing platform optimizations for specification: {SpecificationId}", specification.Id);
                
                // Return default platform optimization recommendation
                return new PlatformOptimizationRecommendation(
                    specification.TargetPlatform,
                    new List<OptimizationRecommendation>
                    {
                        new OptimizationRecommendation(OptimizationType.Performance, "Default performance optimization", "Standard performance improvements")
                    }
                );
            }
        }

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

        private async Task<string> CallAIAsync(string prompt, CancellationToken cancellationToken)
        {
            var request = new Nexo.Feature.AI.Models.ModelRequest(0.7, 0.0, 0.0, false)
            {
                Input = prompt,
                SystemPrompt = "You are a software architecture expert. Analyze the given feature specification and provide structured analysis results. Return only valid JSON without any additional text or explanations.",
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var response = await _modelOrchestrator.ExecuteAsync(request, cancellationToken);
            return response.Content;
        }

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
