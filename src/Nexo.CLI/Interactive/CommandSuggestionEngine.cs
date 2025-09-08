using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Intelligent command suggestion engine with context awareness and AI-powered recommendations
    /// </summary>
    public class CommandSuggestionEngine : ICommandSuggestionEngine
    {
        private readonly IModelOrchestrator _aiOrchestrator;
        private readonly ILogger<CommandSuggestionEngine> _logger;
        
        // Command registry for available commands
        private readonly Dictionary<string, CommandInfo> _availableCommands;
        
        public CommandSuggestionEngine(
            IModelOrchestrator aiOrchestrator,
            ILogger<CommandSuggestionEngine> logger)
        {
            _aiOrchestrator = aiOrchestrator;
            _logger = logger;
            _availableCommands = InitializeCommandRegistry();
        }
        
        public async Task<IEnumerable<string>> GetCompletionsAsync(string partialInput)
        {
            var tokens = partialInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (tokens.Length == 0)
            {
                return await GetTopLevelCommands();
            }
            
            if (tokens.Length == 1)
            {
                return await GetMatchingCommands(tokens[0]);
            }
            
            return await GetParameterCompletions(tokens);
        }
        
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
        
        private async Task<IEnumerable<string>> GetTopLevelCommands()
        {
            return _availableCommands.Keys.OrderBy(k => k);
        }
        
        private async Task<IEnumerable<string>> GetMatchingCommands(string partialCommand)
        {
            return _availableCommands.Keys
                .Where(cmd => cmd.StartsWith(partialCommand, StringComparison.OrdinalIgnoreCase))
                .OrderBy(cmd => cmd);
        }
        
        private async Task<IEnumerable<string>> GetParameterCompletions(string[] tokens)
        {
            // This would be enhanced to provide parameter-specific completions
            // For now, return empty list
            return new List<string>();
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
        
        private Dictionary<string, CommandInfo> InitializeCommandRegistry()
        {
            return new Dictionary<string, CommandInfo>
            {
                ["project"] = new CommandInfo
                {
                    Name = "project",
                    Description = "Project management and scaffolding",
                    Category = "project",
                    SubCommands = new[] { "init", "scaffold", "template", "env" }
                },
                ["analyze"] = new CommandInfo
                {
                    Name = "analyze",
                    Description = "Code and performance analysis",
                    Category = "analyze",
                    SubCommands = new[] { "code", "performance", "architecture" }
                },
                ["optimize"] = new CommandInfo
                {
                    Name = "optimize",
                    Description = "Performance optimization",
                    Category = "optimize",
                    SubCommands = new[] { "performance", "memory", "build" }
                },
                ["test"] = new CommandInfo
                {
                    Name = "test",
                    Description = "Testing and validation",
                    Category = "test",
                    SubCommands = new[] { "run", "coverage", "generate" }
                },
                ["generate"] = new CommandInfo
                {
                    Name = "generate",
                    Description = "Code and feature generation",
                    Category = "generate",
                    SubCommands = new[] { "code", "tests", "docs" }
                },
                ["iteration"] = new CommandInfo
                {
                    Name = "iteration",
                    Description = "Iteration strategy management",
                    Category = "iteration",
                    SubCommands = new[] { "create", "execute", "analyze" }
                },
                ["unity"] = new CommandInfo
                {
                    Name = "unity",
                    Description = "Unity game development",
                    Category = "unity",
                    SubCommands = new[] { "project", "build", "test" }
                },
                ["adaptation"] = new CommandInfo
                {
                    Name = "adaptation",
                    Description = "Real-time adaptation management",
                    Category = "adaptation",
                    SubCommands = new[] { "enable", "disable", "status" }
                },
                ["pipeline"] = new CommandInfo
                {
                    Name = "pipeline",
                    Description = "Workflow and pipeline management",
                    Category = "pipeline",
                    SubCommands = new[] { "create", "execute", "validate" }
                },
                ["web"] = new CommandInfo
                {
                    Name = "web",
                    Description = "Web development and optimization",
                    Category = "web",
                    SubCommands = new[] { "generate", "optimize", "build" }
                },
                ["interactive"] = new CommandInfo
                {
                    Name = "interactive",
                    Description = "Interactive development sessions",
                    Category = "interactive",
                    SubCommands = new[] { "chat", "session", "live" }
                },
                ["help"] = new CommandInfo
                {
                    Name = "help",
                    Description = "Show help information",
                    Category = "system",
                    SubCommands = new string[0]
                },
                ["status"] = new CommandInfo
                {
                    Name = "status",
                    Description = "Show system status",
                    Category = "system",
                    SubCommands = new string[0]
                },
                ["dashboard"] = new CommandInfo
                {
                    Name = "dashboard",
                    Description = "Open real-time dashboard",
                    Category = "system",
                    SubCommands = new string[0]
                }
            };
        }
    }
    
    /// <summary>
    /// Information about a command for the registry
    /// </summary>
    public class CommandInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string[] SubCommands { get; set; } = Array.Empty<string>();
    }
}
