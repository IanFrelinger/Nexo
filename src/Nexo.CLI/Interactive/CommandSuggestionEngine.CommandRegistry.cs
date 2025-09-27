using System;
using System.Collections.Generic;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Command registry functionality for command suggestion engine.
    /// </summary>
    public partial class CommandSuggestionEngine
    {
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
}
