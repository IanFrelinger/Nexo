using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Unity.AI.Agents
{
    /// <summary>
    /// Game mechanics generation functionality for GameMechanicsGenerationAgent.
    /// Handles core mechanics generation and AI processing.
    /// </summary>
    public partial class GameMechanicsGenerationAgent
    {
        /// <summary>
        /// Generates game mechanics based on requirements using AI.
        /// </summary>
        private async Task<GeneratedGameMechanics> GenerateGameMechanics(MechanicsGenerationRequest request)
        {
            var prompt = $"""
            Design game mechanics for this requirement:
            
            Game Type: {request.GameType}
            Core Mechanics Needed: {request.RequiredMechanics}
            Target Audience: {request.TargetAudience}
            Platform: Unity (Mobile/PC/Console)
            Performance Requirements: {request.PerformanceRequirements}
            
            Generate:
            1. Core mechanic systems with clear rules
            2. Player interaction patterns
            3. Progression systems
            4. Balance considerations
            5. Technical implementation approach
            6. Performance optimization strategies
            
            Design for {request.TargetPlatform} with emphasis on:
            - Smooth 60 FPS gameplay
            - Intuitive controls
            - Scalable difficulty
            - Engaging progression
            
            Provide detailed technical specifications for Unity implementation.
            """;
            
            var modelRequest = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 1500,
                Temperature = 0.7
            };
            
            var response = await _modelOrchestrator.ProcessAsync(modelRequest);
            return ParseGeneratedMechanics(response.Response);
        }

        /// <summary>
        /// Parses AI response to extract generated game mechanics.
        /// </summary>
        private GeneratedGameMechanics ParseGeneratedMechanics(string aiResponse)
        {
            var mechanics = new GeneratedGameMechanics
            {
                Mechanics = new List<GameMechanic>(),
                TechnicalSpecifications = aiResponse,
                PerformanceStrategies = ExtractPerformanceStrategies(aiResponse)
            };
            
            // Parse mechanics from AI response
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3."))
                {
                    mechanics.Mechanics.Add(new GameMechanic
                    {
                        Name = ExtractMechanicName(line),
                        Description = line.Substring(2).Trim(),
                        Rules = ExtractRules(line),
                        PerformanceRequirements = "60 FPS target",
                        RequiresUI = line.ToLower().Contains("ui") || line.ToLower().Contains("interface"),
                        RequiresConfiguration = line.ToLower().Contains("config") || line.ToLower().Contains("setting"),
                        RequiresStateManagement = line.ToLower().Contains("state") || line.ToLower().Contains("data")
                    });
                }
            }
            
            return mechanics;
        }

        /// <summary>
        /// Extracts mechanic name from AI response line.
        /// </summary>
        private string ExtractMechanicName(string line)
        {
            // Extract mechanic name from line
            var parts = line.Split(':');
            if (parts.Length > 1)
            {
                return parts[1].Trim().Split(' ')[0];
            }
            
            return "GeneratedMechanic";
        }

        /// <summary>
        /// Extracts rules from AI response line.
        /// </summary>
        private List<string> ExtractRules(string line)
        {
            // Extract rules from line
            var rules = new List<string>();
            
            if (line.Contains("rule"))
            {
                rules.Add("Follow game design principles");
            }
            
            if (line.Contains("balance"))
            {
                rules.Add("Maintain game balance");
            }
            
            return rules;
        }

        /// <summary>
        /// Extracts performance strategies from AI response.
        /// </summary>
        private List<string> ExtractPerformanceStrategies(string response)
        {
            var strategies = new List<string>();
            var lines = response.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("performance") || line.ToLower().Contains("optimization"))
                {
                    strategies.Add(line.Trim());
                }
            }
            
            return strategies;
        }
    }
}
