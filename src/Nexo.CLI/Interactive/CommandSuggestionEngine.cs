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
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class CommandSuggestionEngine : ICommandSuggestionEngine
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