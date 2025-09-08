using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Generates comprehensive documentation for commands and features
    /// </summary>
    public class CommandDocumentationGenerator : IDocumentationGenerator
    {
        private readonly IModelOrchestrator _aiOrchestrator;
        private readonly ILogger<CommandDocumentationGenerator> _logger;
        private readonly Dictionary<string, CommandDocumentation> _commandDocs;
        
        public CommandDocumentationGenerator(
            IModelOrchestrator aiOrchestrator,
            ILogger<CommandDocumentationGenerator> logger)
        {
            _aiOrchestrator = aiOrchestrator;
            _logger = logger;
            _commandDocs = InitializeCommandDocumentation();
        }
        
        public async Task<string> GenerateCommandDocumentationAsync(string commandName)
        {
            if (_commandDocs.TryGetValue(commandName, out var doc))
            {
                return await GenerateDocumentationFromTemplate(doc);
            }
            
            // Try to generate documentation using AI
            try
            {
                return await GenerateAIDocumentation(commandName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI documentation for command: {Command}", commandName);
                return $"Command '{commandName}' not found. Use 'nexo help' to see available commands.";
            }
        }
        
        public async Task<IEnumerable<DocumentationResult>> SearchDocumentationAsync(string searchTerm)
        {
            var results = new List<DocumentationResult>();
            
            // Search in command documentation
            foreach (var doc in _commandDocs.Values)
            {
                var relevance = CalculateRelevance(doc, searchTerm);
                if (relevance > 0.1) // Only include relevant results
                {
                    results.Add(new DocumentationResult
                    {
                        Title = doc.Name,
                        Summary = doc.Description,
                        Category = doc.Category,
                        Relevance = relevance,
                        Content = doc.Description,
                        Url = $"nexo {doc.Name} --help"
                    });
                }
            }
            
            // Search in topics
            var topics = await GetAvailableTopicsAsync();
            foreach (var topic in topics)
            {
                var relevance = CalculateTopicRelevance(topic, searchTerm);
                if (relevance > 0.1)
                {
                    results.Add(new DocumentationResult
                    {
                        Title = topic.Name,
                        Summary = topic.Description,
                        Category = topic.Category,
                        Relevance = relevance,
                        Content = topic.Description,
                        Url = $"nexo help {topic.Name.ToLower().Replace(" ", "-")}"
                    });
                }
            }
            
            return results.OrderByDescending(r => r.Relevance);
        }
        
        public async Task<IEnumerable<DocumentationTopic>> GetAvailableTopicsAsync()
        {
            return new List<DocumentationTopic>
            {
                new DocumentationTopic
                {
                    Name = "Getting Started",
                    Description = "Learn the basics of using Nexo CLI",
                    Category = "Tutorial",
                    Keywords = new List<string> { "getting started", "basics", "tutorial", "beginner" }
                },
                new DocumentationTopic
                {
                    Name = "Project Management",
                    Description = "Create and manage projects with Nexo",
                    Category = "Project",
                    Keywords = new List<string> { "project", "init", "scaffold", "template" }
                },
                new DocumentationTopic
                {
                    Name = "Code Generation",
                    Description = "Generate code using AI and templates",
                    Category = "Generation",
                    Keywords = new List<string> { "generate", "code", "ai", "template" }
                },
                new DocumentationTopic
                {
                    Name = "Performance Optimization",
                    Description = "Optimize application performance",
                    Category = "Performance",
                    Keywords = new List<string> { "performance", "optimize", "monitor", "analyze" }
                },
                new DocumentationTopic
                {
                    Name = "Unity Game Development",
                    Description = "Unity-specific development tools",
                    Category = "Unity",
                    Keywords = new List<string> { "unity", "game", "development", "build" }
                },
                new DocumentationTopic
                {
                    Name = "Real-Time Adaptation",
                    Description = "Automatic adaptation and optimization",
                    Category = "Adaptation",
                    Keywords = new List<string> { "adaptation", "real-time", "optimization", "learning" }
                },
                new DocumentationTopic
                {
                    Name = "Pipeline Management",
                    Description = "Create and manage development pipelines",
                    Category = "Pipeline",
                    Keywords = new List<string> { "pipeline", "workflow", "ci", "cd" }
                },
                new DocumentationTopic
                {
                    Name = "Interactive Mode",
                    Description = "Use Nexo's interactive features",
                    Category = "Interactive",
                    Keywords = new List<string> { "interactive", "dashboard", "monitor", "real-time" }
                }
            };
        }
        
        private async Task<string> GenerateDocumentationFromTemplate(CommandDocumentation doc)
        {
            var documentation = new System.Text.StringBuilder();
            
            // Header
            documentation.AppendLine($"# {doc.Name}");
            documentation.AppendLine($"{doc.Description}");
            documentation.AppendLine();
            
            // Usage
            documentation.AppendLine("## Usage");
            documentation.AppendLine($"```");
            documentation.AppendLine($"nexo {doc.UsageExample}");
            documentation.AppendLine($"```");
            documentation.AppendLine();
            
            // Parameters
            if (doc.Parameters.Any())
            {
                documentation.AppendLine("## Parameters");
                foreach (var param in doc.Parameters)
                {
                    documentation.AppendLine($"### `{param.Name}`");
                    documentation.AppendLine($"- **Type**: {param.Type}");
                    documentation.AppendLine($"- **Required**: {(param.Required ? "Yes" : "No")}");
                    documentation.AppendLine($"- **Description**: {param.Description}");
                    if (param.DefaultValue != null)
                    {
                        documentation.AppendLine($"- **Default**: {param.DefaultValue}");
                    }
                    documentation.AppendLine();
                }
            }
            
            // Examples
            if (doc.Examples.Any())
            {
                documentation.AppendLine("## Examples");
                foreach (var example in doc.Examples)
                {
                    documentation.AppendLine($"### {example.Title}");
                    documentation.AppendLine(example.Description);
                    documentation.AppendLine("```bash");
                    documentation.AppendLine(example.CommandLine);
                    documentation.AppendLine("```");
                    if (!string.IsNullOrEmpty(example.ExpectedOutput))
                    {
                        documentation.AppendLine("**Expected Output:**");
                        documentation.AppendLine("```");
                        documentation.AppendLine(example.ExpectedOutput);
                        documentation.AppendLine("```");
                    }
                    documentation.AppendLine();
                }
            }
            
            // Related commands
            if (doc.RelatedCommands.Any())
            {
                documentation.AppendLine("## Related Commands");
                foreach (var related in doc.RelatedCommands)
                {
                    documentation.AppendLine($"- `nexo {related.Name}` - {related.Description}");
                }
                documentation.AppendLine();
            }
            
            return documentation.ToString();
        }
        
        private async Task<string> GenerateAIDocumentation(string commandName)
        {
            var prompt = $"""
            Generate comprehensive documentation for the Nexo CLI command '{commandName}'.
            
            Include:
            1. Command description and purpose
            2. Usage syntax with examples
            3. Available parameters and options
            4. Practical examples
            5. Related commands
            
            Format the output in Markdown.
            """;
            
            var request = new ModelRequest
            {
                Input = prompt,
                ModelType = ModelType.TextGeneration,
                MaxTokens = 1000,
                Temperature = 0.3
            };
            
            var response = await _aiOrchestrator.ProcessAsync(request);
            return response.Response;
        }
        
        private double CalculateRelevance(CommandDocumentation doc, string searchTerm)
        {
            var term = searchTerm.ToLower();
            var relevance = 0.0;
            
            // Check name
            if (doc.Name.ToLower().Contains(term))
                relevance += 1.0;
            
            // Check description
            if (doc.Description.ToLower().Contains(term))
                relevance += 0.8;
            
            // Check category
            if (doc.Category.ToLower().Contains(term))
                relevance += 0.6;
            
            // Check parameters
            foreach (var param in doc.Parameters)
            {
                if (param.Name.ToLower().Contains(term) || param.Description.ToLower().Contains(term))
                    relevance += 0.4;
            }
            
            return Math.Min(relevance, 1.0);
        }
        
        private double CalculateTopicRelevance(DocumentationTopic topic, string searchTerm)
        {
            var term = searchTerm.ToLower();
            var relevance = 0.0;
            
            // Check name
            if (topic.Name.ToLower().Contains(term))
                relevance += 1.0;
            
            // Check description
            if (topic.Description.ToLower().Contains(term))
                relevance += 0.8;
            
            // Check keywords
            foreach (var keyword in topic.Keywords)
            {
                if (keyword.ToLower().Contains(term))
                    relevance += 0.6;
            }
            
            return Math.Min(relevance, 1.0);
        }
        
        private Dictionary<string, CommandDocumentation> InitializeCommandDocumentation()
        {
            return new Dictionary<string, CommandDocumentation>
            {
                ["project"] = new CommandDocumentation
                {
                    Name = "project",
                    Description = "Project management and scaffolding commands",
                    Category = "Project",
                    UsageExample = "project init --name MyProject --type webapi",
                    Parameters = new List<CommandParameter>
                    {
                        new CommandParameter { Name = "init", Type = "Command", Required = false, Description = "Initialize a new project" },
                        new CommandParameter { Name = "--name", Type = "String", Required = true, Description = "Project name" },
                        new CommandParameter { Name = "--type", Type = "String", Required = true, Description = "Project type (webapi, console, library)" },
                        new CommandParameter { Name = "--ai", Type = "Boolean", Required = false, Description = "Use AI-enhanced initialization" }
                    },
                    Examples = new List<CommandExample>
                    {
                        new CommandExample
                        {
                            Title = "Create a Web API Project",
                            Description = "Initialize a new ASP.NET Core Web API project with AI enhancement",
                            CommandLine = "nexo project init --name MyApi --type webapi --ai"
                        }
                    },
                    RelatedCommands = new List<CommandReference>
                    {
                        new CommandReference { Name = "scaffold", Description = "Scaffold code and project structure" },
                        new CommandReference { Name = "template", Description = "Manage project templates" }
                    }
                },
                ["analyze"] = new CommandDocumentation
                {
                    Name = "analyze",
                    Description = "Analyze code for quality and architectural compliance",
                    Category = "Analysis",
                    UsageExample = "analyze --path ./src --type performance",
                    Parameters = new List<CommandParameter>
                    {
                        new CommandParameter { Name = "--path", Type = "String", Required = true, Description = "Path to analyze" },
                        new CommandParameter { Name = "--type", Type = "String", Required = false, Description = "Analysis type (performance, architecture, quality)" },
                        new CommandParameter { Name = "--output", Type = "String", Required = false, Description = "Output format (json, text)" }
                    },
                    Examples = new List<CommandExample>
                    {
                        new CommandExample
                        {
                            Title = "Analyze Project Performance",
                            Description = "Analyze the entire project for performance issues",
                            CommandLine = "nexo analyze --path . --type performance --output json"
                        }
                    }
                },
                ["interactive"] = new CommandDocumentation
                {
                    Name = "interactive",
                    Description = "Start interactive CLI mode with intelligent suggestions",
                    Category = "Interactive",
                    UsageExample = "interactive",
                    Parameters = new List<CommandParameter>(),
                    Examples = new List<CommandExample>
                    {
                        new CommandExample
                        {
                            Title = "Start Interactive Mode",
                            Description = "Launch the interactive CLI with smart prompts and suggestions",
                            CommandLine = "nexo interactive"
                        }
                    }
                },
                ["dashboard"] = new CommandDocumentation
                {
                    Name = "dashboard",
                    Description = "Open real-time monitoring dashboard",
                    Category = "Monitoring",
                    UsageExample = "dashboard",
                    Parameters = new List<CommandParameter>(),
                    Examples = new List<CommandExample>
                    {
                        new CommandExample
                        {
                            Title = "Open Dashboard",
                            Description = "Launch the real-time monitoring dashboard",
                            CommandLine = "nexo dashboard"
                        }
                    }
                }
            };
        }
    }
    
    /// <summary>
    /// Represents command documentation
    /// </summary>
    public class CommandDocumentation
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string UsageExample { get; set; } = string.Empty;
        public List<CommandParameter> Parameters { get; set; } = new();
        public List<CommandExample> Examples { get; set; } = new();
        public List<CommandReference> RelatedCommands { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a command parameter
    /// </summary>
    public class CommandParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DefaultValue { get; set; }
    }
    
    /// <summary>
    /// Represents a command reference
    /// </summary>
    public class CommandReference
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
