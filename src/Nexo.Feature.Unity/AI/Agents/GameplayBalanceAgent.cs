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
    /// AI agent specialized in game balance analysis and recommendations
    /// </summary>
    public class GameplayBalanceAgent : ISpecializedAgent
    {
        public string AgentId => "GameplayBalance";
        public AgentSpecialization Specialization => AgentSpecialization.GameDevelopment | AgentSpecialization.PerformanceOptimization;
        public PlatformCompatibility PlatformExpertise => PlatformCompatibility.Unity;
        
        private readonly IGameplayAnalyzer _gameplayAnalyzer;
        private readonly IBalanceCalculator _balanceCalculator;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger<GameplayBalanceAgent> _logger;
        
        public GameplayBalanceAgent(
            IGameplayAnalyzer gameplayAnalyzer,
            IBalanceCalculator balanceCalculator,
            IModelOrchestrator modelOrchestrator,
            ILogger<GameplayBalanceAgent> logger)
        {
            _gameplayAnalyzer = gameplayAnalyzer;
            _balanceCalculator = balanceCalculator;
            _modelOrchestrator = modelOrchestrator;
            _logger = logger;
        }
        
        public async Task<AgentResponse> ProcessAsync(AgentRequest request)
        {
            _logger.LogInformation("Processing gameplay balance analysis request");
            
            try
            {
                var gameplayContext = request.Context.GetGameplayContext();
                
                // Analyze current game balance
                var balanceAnalysis = await _gameplayAnalyzer.AnalyzeGameplayBalanceAsync(gameplayContext);
                
                if (balanceAnalysis.HasBalanceIssues)
                {
                    // Generate balance recommendations using AI
                    var balanceRecommendations = await GenerateBalanceRecommendations(balanceAnalysis);
                    
                    // Create balanced game mechanics
                    var balancedMechanics = await CreateBalancedMechanics(balanceRecommendations);
                    
                    return new AgentResponse
                    {
                        Result = balancedMechanics,
                        Confidence = 0.85,
                        Metadata = new Dictionary<string, object>
                        {
                            ["BalanceIssues"] = balanceAnalysis.Issues,
                            ["Recommendations"] = balanceRecommendations,
                            ["BalanceScore"] = balanceAnalysis.OverallBalanceScore
                        }
                    };
                }
                
                return AgentResponse.BalanceIsOptimal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process gameplay balance analysis");
                return AgentResponse.CreateErrorResponse(ex.Message);
            }
        }
        
        public async Task<AgentResponse> CoordinateAsync(AgentRequest request, IEnumerable<ISpecializedAgent> collaborators)
        {
            _logger.LogInformation("Coordinating gameplay balance analysis with other agents");
            
            try
            {
                // Coordinate with Unity optimization agent for performance-balanced mechanics
                var unityAgent = collaborators.FirstOrDefault(a => a.AgentId == "UnityOptimization");
                
                if (unityAgent != null)
                {
                    // Get performance implications of balance changes
                    var performanceAnalysis = await unityAgent.ProcessAsync(
                        request.CreatePerformanceAnalysisRequest());
                    
                    // Integrate performance considerations into balance recommendations
                    var balanceResponse = await ProcessAsync(request);
                    
                    return new AgentResponse
                    {
                        Result = IntegratePerformanceAndBalance(balanceResponse, performanceAnalysis),
                        Confidence = Math.Min(balanceResponse.Confidence, performanceAnalysis.Confidence),
                        Metadata = MergeMetadata(balanceResponse.Metadata, performanceAnalysis.Metadata)
                    };
                }
                
                return await ProcessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to coordinate gameplay balance analysis");
                return AgentResponse.CreateErrorResponse(ex.Message);
            }
        }
        
        private async Task<BalanceRecommendations> GenerateBalanceRecommendations(GameplayBalanceAnalysis analysis)
        {
            var prompt = $"""
            Analyze this game balance data and provide recommendations:
            
            Game Type: {analysis.GameType}
            Player Count: {analysis.PlayerCount}
            Current Balance Issues:
            {string.Join("\n", analysis.Issues.Select(i => $"- {i.Description} (Severity: {i.Severity})"))}
            
            Player Performance Data:
            - Average Win Rate: {analysis.AverageWinRate:P}
            - Skill Variance: {analysis.SkillVariance:F2}
            - Most Used Strategies: {string.Join(", ", analysis.PopularStrategies)}
            - Least Used Strategies: {string.Join(", ", analysis.UnderusedStrategies)}
            
            Provide specific, actionable balance recommendations:
            1. Numerical adjustments (damage, health, costs, etc.)
            2. Mechanical changes (cooldowns, range, effects)
            3. New mechanics to address imbalances
            4. Player progression adjustments
            
            Focus on maintaining fun while improving competitive balance.
            """;
            
            var request = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 1000,
                Temperature = 0.7
            };
            
            var response = await _modelOrchestrator.ProcessAsync(request);
            return ParseBalanceRecommendations(response.Response);
        }
        
        private async Task<BalancedGameMechanics> CreateBalancedMechanics(BalanceRecommendations recommendations)
        {
            var mechanics = new BalancedGameMechanics
            {
                Recommendations = recommendations,
                ImplementationGuidance = await GenerateImplementationGuidance(recommendations),
                TestingStrategy = await GenerateTestingStrategy(recommendations)
            };
            
            return mechanics;
        }
        
        private async Task<ImplementationGuidance> GenerateImplementationGuidance(BalanceRecommendations recommendations)
        {
            var prompt = $"""
            Provide Unity implementation guidance for these game balance changes:
            
            {string.Join("\n", recommendations.Changes.Select(c => $"- {c.Description}"))}
            
            Include:
            1. Unity-specific implementation steps
            2. Performance considerations
            3. Code examples for common changes
            4. Testing approaches
            5. Rollback strategies
            
            Focus on maintainable, performant Unity code.
            """;
            
            var request = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 800,
                Temperature = 0.5
            };
            
            var response = await _modelOrchestrator.ProcessAsync(request);
            return ParseImplementationGuidance(response.Response);
        }
        
        private async Task<TestingStrategy> GenerateTestingStrategy(BalanceRecommendations recommendations)
        {
            var prompt = $"""
            Design a testing strategy for these game balance changes:
            
            {string.Join("\n", recommendations.Changes.Select(c => $"- {c.Description}"))}
            
            Include:
            1. A/B testing approach
            2. Metrics to track
            3. Player feedback collection
            4. Performance monitoring
            5. Rollback criteria
            
            Focus on data-driven balance validation.
            """;
            
            var request = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 600,
                Temperature = 0.5
            };
            
            var response = await _modelOrchestrator.ProcessAsync(request);
            return ParseTestingStrategy(response.Response);
        }
        
        private BalanceRecommendations ParseBalanceRecommendations(string aiResponse)
        {
            var recommendations = new BalanceRecommendations
            {
                Changes = new List<BalanceChange>(),
                OverallStrategy = "AI-generated balance recommendations"
            };
            
            try
            {
                var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") || line.StartsWith("4."))
                    {
                        recommendations.Changes.Add(new BalanceChange
                        {
                            Type = DetermineChangeType(line),
                            Description = line.Substring(2).Trim(),
                            Priority = BalanceChangePriority.Medium
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse balance recommendations from AI response");
            }
            
            return recommendations;
        }
        
        private ImplementationGuidance ParseImplementationGuidance(string aiResponse)
        {
            return new ImplementationGuidance
            {
                Steps = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.Trim().Length > 0)
                    .ToList(),
                CodeExamples = ExtractCodeExamples(aiResponse),
                PerformanceNotes = ExtractPerformanceNotes(aiResponse)
            };
        }
        
        private TestingStrategy ParseTestingStrategy(string aiResponse)
        {
            return new TestingStrategy
            {
                Approach = "A/B Testing with Performance Monitoring",
                Metrics = ExtractMetrics(aiResponse),
                RollbackCriteria = ExtractRollbackCriteria(aiResponse)
            };
        }
        
        private BalanceChangeType DetermineChangeType(string line)
        {
            var lowerLine = line.ToLower();
            
            if (lowerLine.Contains("damage") || lowerLine.Contains("health") || lowerLine.Contains("cost"))
                return BalanceChangeType.NumericalAdjustment;
            
            if (lowerLine.Contains("cooldown") || lowerLine.Contains("range") || lowerLine.Contains("effect"))
                return BalanceChangeType.MechanicalChange;
            
            if (lowerLine.Contains("new") || lowerLine.Contains("add"))
                return BalanceChangeType.NewMechanic;
            
            return BalanceChangeType.GeneralAdjustment;
        }
        
        private List<string> ExtractCodeExamples(string response)
        {
            // Simple extraction of code-like content
            var examples = new List<string>();
            var lines = response.Split('\n');
            
            bool inCodeBlock = false;
            var currentExample = new List<string>();
            
            foreach (var line in lines)
            {
                if (line.Contains("```") || line.Contains("code"))
                {
                    if (inCodeBlock && currentExample.Any())
                    {
                        examples.Add(string.Join("\n", currentExample));
                        currentExample.Clear();
                    }
                    inCodeBlock = !inCodeBlock;
                }
                else if (inCodeBlock)
                {
                    currentExample.Add(line);
                }
            }
            
            return examples;
        }
        
        private List<string> ExtractPerformanceNotes(string response)
        {
            var notes = new List<string>();
            var lines = response.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("performance") || line.ToLower().Contains("optimization"))
                {
                    notes.Add(line.Trim());
                }
            }
            
            return notes;
        }
        
        private List<string> ExtractMetrics(string response)
        {
            var metrics = new List<string>();
            var lines = response.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("metric") || line.ToLower().Contains("track"))
                {
                    metrics.Add(line.Trim());
                }
            }
            
            return metrics;
        }
        
        private List<string> ExtractRollbackCriteria(string response)
        {
            var criteria = new List<string>();
            var lines = response.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("rollback") || line.ToLower().Contains("revert"))
                {
                    criteria.Add(line.Trim());
                }
            }
            
            return criteria;
        }
        
        private object IntegratePerformanceAndBalance(AgentResponse balanceResponse, AgentResponse performanceResponse)
        {
            // Integrate performance considerations into balance recommendations
            return new
            {
                BalanceRecommendations = balanceResponse.Result,
                PerformanceConsiderations = performanceResponse.Result,
                IntegratedApproach = "Performance-optimized balance changes"
            };
        }
        
        private Dictionary<string, object> MergeMetadata(Dictionary<string, object> balanceMetadata, Dictionary<string, object> performanceMetadata)
        {
            var merged = new Dictionary<string, object>(balanceMetadata);
            
            foreach (var kvp in performanceMetadata)
            {
                merged[$"Performance_{kvp.Key}"] = kvp.Value;
            }
            
            return merged;
        }
    }
    
    /// <summary>
    /// Gameplay analyzer interface
    /// </summary>
    public interface IGameplayAnalyzer
    {
        Task<GameplayBalanceAnalysis> AnalyzeGameplayBalanceAsync(GameplayContext context);
    }
    
    /// <summary>
    /// Balance calculator interface
    /// </summary>
    public interface IBalanceCalculator
    {
        Task<double> CalculateBalanceScoreAsync(GameplayData data);
        Task<IEnumerable<BalanceIssue>> IdentifyBalanceIssuesAsync(GameplayData data);
    }
    
    /// <summary>
    /// Gameplay balance analysis
    /// </summary>
    public class GameplayBalanceAnalysis
    {
        public string GameType { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public double AverageWinRate { get; set; }
        public double SkillVariance { get; set; }
        public IEnumerable<string> PopularStrategies { get; set; } = new List<string>();
        public IEnumerable<string> UnderusedStrategies { get; set; } = new List<string>();
        public IEnumerable<BalanceIssue> Issues { get; set; } = new List<BalanceIssue>();
        public double OverallBalanceScore { get; set; }
        public bool HasBalanceIssues => Issues.Any();
    }
    
    /// <summary>
    /// Balance issue
    /// </summary>
    public class BalanceIssue
    {
        public string Description { get; set; } = string.Empty;
        public BalanceIssueSeverity Severity { get; set; }
        public string AffectedSystem { get; set; } = string.Empty;
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Balance recommendations
    /// </summary>
    public class BalanceRecommendations
    {
        public string OverallStrategy { get; set; } = string.Empty;
        public IEnumerable<BalanceChange> Changes { get; set; } = new List<BalanceChange>();
    }
    
    /// <summary>
    /// Balance change
    /// </summary>
    public class BalanceChange
    {
        public BalanceChangeType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public BalanceChangePriority Priority { get; set; }
    }
    
    /// <summary>
    /// Balanced game mechanics
    /// </summary>
    public class BalancedGameMechanics
    {
        public BalanceRecommendations Recommendations { get; set; } = new();
        public ImplementationGuidance ImplementationGuidance { get; set; } = new();
        public TestingStrategy TestingStrategy { get; set; } = new();
    }
    
    /// <summary>
    /// Implementation guidance
    /// </summary>
    public class ImplementationGuidance
    {
        public IEnumerable<string> Steps { get; set; } = new List<string>();
        public IEnumerable<string> CodeExamples { get; set; } = new List<string>();
        public IEnumerable<string> PerformanceNotes { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Testing strategy
    /// </summary>
    public class TestingStrategy
    {
        public string Approach { get; set; } = string.Empty;
        public IEnumerable<string> Metrics { get; set; } = new List<string>();
        public IEnumerable<string> RollbackCriteria { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Gameplay context
    /// </summary>
    public class GameplayContext
    {
        public string GameType { get; set; } = string.Empty;
        public GameplayData? Data { get; set; }
        public string ProjectPath { get; set; } = string.Empty;
    }
    
    // Enums
    public enum BalanceIssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum BalanceChangeType
    {
        NumericalAdjustment,
        MechanicalChange,
        NewMechanic,
        GeneralAdjustment
    }
    
    public enum BalanceChangePriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
