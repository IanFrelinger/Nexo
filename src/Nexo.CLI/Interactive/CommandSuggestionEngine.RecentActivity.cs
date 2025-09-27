using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Recent activity suggestions functionality for command suggestion engine.
    /// </summary>
    public partial class CommandSuggestionEngine
    {
        public async Task<IEnumerable<CommandSuggestion>> GetRecentActivitySuggestionsAsync(CLIContext context)
        {
            var suggestions = new List<CommandSuggestion>();
            
            // Analyze recent commands to suggest follow-up actions
            var recentCommands = context.RecentCommands.Take(5).ToList();
            
            if (recentCommands.Any(cmd => cmd.Contains("project init")))
            {
                suggestions.Add(new CommandSuggestion
                {
                    Command = "project scaffold --type controller --name HomeController",
                    Description = "Scaffold a controller for your new project",
                    Category = "project",
                    Relevance = 0.9,
                    Reason = "You recently initialized a project"
                });
                
                suggestions.Add(new CommandSuggestion
                {
                    Command = "project env --setup",
                    Description = "Set up development environment for your project",
                    Category = "project",
                    Relevance = 0.8,
                    Reason = "Complete project setup"
                });
            }
            
            if (recentCommands.Any(cmd => cmd.Contains("analyze")))
            {
                suggestions.Add(new CommandSuggestion
                {
                    Command = "optimize performance",
                    Description = "Optimize performance based on analysis results",
                    Category = "optimize",
                    Relevance = 0.85,
                    Reason = "Follow up on analysis with optimization"
                });
            }
            
            if (recentCommands.Any(cmd => cmd.Contains("test")))
            {
                suggestions.Add(new CommandSuggestion
                {
                    Command = "test coverage --detailed",
                    Description = "Get detailed test coverage report",
                    Category = "test",
                    Relevance = 0.8,
                    Reason = "Get more detailed testing information"
                });
            }
            
            return suggestions;
        }
    }
}
