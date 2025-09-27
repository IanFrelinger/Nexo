using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Contextual suggestions functionality for command suggestion engine.
    /// </summary>
    public partial class CommandSuggestionEngine
    {
        public async Task<IEnumerable<CommandSuggestion>> GetContextualSuggestionsAsync(CLIContext context)
        {
            var suggestions = new List<CommandSuggestion>();
            
            // Project-based suggestions
            if (context.CurrentProject != null)
            {
                suggestions.AddRange(await GetProjectBasedSuggestions(context.CurrentProject));
            }
            
            // Performance-based suggestions
            if (context.HasPerformanceIssues)
            {
                suggestions.AddRange(await GetPerformanceOptimizationSuggestions(context));
            }
            
            // AI-powered suggestions based on user behavior
            var aiSuggestions = await GetAIPoweredSuggestionsAsync(context);
            suggestions.AddRange(aiSuggestions);
            
            // Recent activity based suggestions
            var recentSuggestions = await GetRecentActivitySuggestionsAsync(context);
            suggestions.AddRange(recentSuggestions);
            
            return suggestions.OrderByDescending(s => s.Relevance).Take(5);
        }
        
        private async Task<IEnumerable<CommandSuggestion>> GetProjectBasedSuggestions(ProjectInfo project)
        {
            var suggestions = new List<CommandSuggestion>();
            
            switch (project.Type.ToLower())
            {
                case "webapi":
                    suggestions.Add(new CommandSuggestion
                    {
                        Command = "project scaffold --type controller --name ApiController",
                        Description = "Scaffold an API controller",
                        Category = "project",
                        Relevance = 0.9,
                        Reason = $"Perfect for {project.Type} project"
                    });
                    break;
                    
                case "console":
                    suggestions.Add(new CommandSuggestion
                    {
                        Command = "project scaffold --type service --name MainService",
                        Description = "Scaffold a service for your console app",
                        Category = "project",
                        Relevance = 0.8,
                        Reason = $"Common pattern for {project.Type} projects"
                    });
                    break;
                    
                case "library":
                    suggestions.Add(new CommandSuggestion
                    {
                        Command = "analyze --path . --type architecture",
                        Description = "Analyze library architecture",
                        Category = "analyze",
                        Relevance = 0.85,
                        Reason = $"Important for {project.Type} projects"
                    });
                    break;
            }
            
            // Common suggestions for all project types
            suggestions.Add(new CommandSuggestion
            {
                Command = "test run --project .",
                Description = "Run tests for the current project",
                Category = "test",
                Relevance = 0.7,
                Reason = "Always good to run tests"
            });
            
            return suggestions;
        }
        
        private async Task<IEnumerable<CommandSuggestion>> GetPerformanceOptimizationSuggestions(CLIContext context)
        {
            return new List<CommandSuggestion>
            {
                new CommandSuggestion
                {
                    Command = "optimize performance --analyze",
                    Description = "Analyze and optimize performance issues",
                    Category = "optimize",
                    Relevance = 0.95,
                    Reason = "Address detected performance issues"
                },
                new CommandSuggestion
                {
                    Command = "monitor start --metrics performance",
                    Description = "Start performance monitoring",
                    Category = "monitor",
                    Relevance = 0.9,
                    Reason = "Monitor performance in real-time"
                },
                new CommandSuggestion
                {
                    Command = "adaptation enable --strategy performance",
                    Description = "Enable performance adaptation strategies",
                    Category = "adaptation",
                    Relevance = 0.85,
                    Reason = "Automatically adapt to performance issues"
                }
            };
        }
    }
}
