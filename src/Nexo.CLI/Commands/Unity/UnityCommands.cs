using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;

namespace Nexo.CLI.Commands.Unity
{
    /// <summary>
    /// Unity-specific CLI commands for game development
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public static partial class UnityCommands
    {
        /// <summary>
        /// Creates the main Unity command with all subcommands
        /// </summary>
        public static Command CreateUnityCommand(IServiceProvider serviceProvider)
        {
            var unityCommand = new Command("unity", "Unity game development tools and optimizations");
            
            // Add subcommands
            unityCommand.AddCommand(CreateAnalyzeCommand(serviceProvider));
            unityCommand.AddCommand(CreateOptimizeCommand(serviceProvider));
            unityCommand.AddCommand(CreateMonitorCommand(serviceProvider));
            unityCommand.AddCommand(CreateBuildOptimizeCommand(serviceProvider));
            
            return unityCommand;
        }
        // This class acts as an orchestrator for various Unity CLI functionalities,
        // with specific categories defined in partial classes.
    }
}