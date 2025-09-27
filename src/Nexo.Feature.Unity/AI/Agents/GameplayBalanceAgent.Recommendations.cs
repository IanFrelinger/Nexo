using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// Balance recommendations functionality
    /// </summary>
    public partial class GameplayBalanceAgent
    {
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
    }
}
