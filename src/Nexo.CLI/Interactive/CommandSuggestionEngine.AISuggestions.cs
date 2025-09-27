using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// AI-powered suggestions functionality for command suggestion engine.
    /// </summary>
    public partial class CommandSuggestionEngine
    {
        public async Task<IEnumerable<CommandSuggestion>> GetAIPoweredSuggestionsAsync(CLIContext context)
        {
            try
            {
                var prompt = $"""
                Based on this CLI context, suggest the most relevant Nexo commands:
                
                Current Project: {context.CurrentProject?.Name ?? "None"}
                Platform: {context.CurrentPlatform ?? "Not specified"}
                Recent Commands: {string.Join(", ", context.RecentCommands.Take(5))}
                Performance Issues: {context.HasPerformanceIssues}
                Active Monitoring: {context.HasActiveMonitoring}
                Working Directory: {context.WorkingDirectory}
                
                Available command categories:
                - project: Project management and analysis
                - generate: Code and feature generation
                - optimize: Performance optimization
                - analyze: Code and performance analysis
                - iteration: Iteration strategy management
                - unity: Unity game development
                - adaptation: Real-time adaptation management
                - pipeline: Workflow and pipeline management
                - test: Testing and validation
                - web: Web development and optimization
                
                Suggest 3-5 most relevant commands with brief explanations.
                Focus on commands that would be most helpful in the current context.
                Return suggestions in this format:
                COMMAND|DESCRIPTION|CATEGORY|REASON
                """;
                
                var request = new ModelRequest
                {
                    Input = prompt,
                    ModelType = ModelType.TextGeneration,
                    MaxTokens = 500,
                    Temperature = 0.7
                };
                
                var response = await _aiOrchestrator.ProcessAsync(request);
                return ParseAISuggestions(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI-powered suggestions");
                return new List<CommandSuggestion>();
            }
        }
        
        private IEnumerable<CommandSuggestion> ParseAISuggestions(string aiResponse)
        {
            var suggestions = new List<CommandSuggestion>();
            
            try
            {
                var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        suggestions.Add(new CommandSuggestion
                        {
                            Command = parts[0].Trim(),
                            Description = parts[1].Trim(),
                            Category = parts[2].Trim(),
                            Relevance = 0.8, // Default relevance for AI suggestions
                            Reason = parts.Length > 3 ? parts[3].Trim() : "AI recommendation"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI suggestions");
            }
            
            return suggestions;
        }
    }
}
