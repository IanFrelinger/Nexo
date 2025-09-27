using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.AI.Agents;
using Nexo.Feature.Unity.Workflows;
using Nexo.Feature.AI.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Game development CLI commands for Unity projects.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class GameDevelopmentCommands
    {
        /// <summary>
        /// Creates the main game development command with all subcommands
        /// </summary>
        public static Command CreateGameDevelopmentCommand(IServiceProvider serviceProvider)
        {
            var gameCommand = new Command("game", "Game development tools and AI-powered features");
            
            // Add subcommands
            gameCommand.AddCommand(CreateGenerateCommand(serviceProvider));
            gameCommand.AddCommand(CreateBalanceCommand(serviceProvider));
            gameCommand.AddCommand(CreateWorkflowCommand(serviceProvider));
            gameCommand.AddCommand(CreateTestCommand(serviceProvider));
            
            return gameCommand;
        }
        // This class acts as an orchestrator for various game development functionalities,
        // with specific categories defined in partial classes.
    }
}