using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using Nexo.CLI.Commands.Adaptation.Commands;
using Nexo.CLI.Commands.Adaptation.Handlers;

namespace Nexo.CLI.Commands.Adaptation.Core
{
    /// <summary>
    /// Main orchestrator for adaptation command functionality
    /// </summary>
    [Command("adaptation")]
    public class AdaptationCommands
    {
        private readonly IServiceProvider _serviceProvider;
        
        public AdaptationCommands(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        /// <summary>
        /// Create the adaptation command group
        /// </summary>
        public static Command CreateAdaptationCommand(IServiceProvider serviceProvider)
        {
            var adaptationCommand = new Command("adaptation", "Real-time adaptation management commands");
            
            // Create command handlers
            var statusHandler = new StatusCommandHandler(serviceProvider);
            var monitorHandler = new MonitorCommandHandler(serviceProvider);
            var triggerHandler = new TriggerCommandHandler(serviceProvider);
            var learningHandler = new LearningCommandHandler(serviceProvider);
            var effectivenessHandler = new EffectivenessCommandHandler(serviceProvider);
            var dashboardHandler = new DashboardCommandHandler(serviceProvider);
            var environmentHandler = new EnvironmentCommandHandler(serviceProvider);
            var engineHandler = new EngineCommandHandler(serviceProvider);
            
            // Add subcommands
            adaptationCommand.AddCommand(statusHandler.CreateStatusCommand());
            adaptationCommand.AddCommand(monitorHandler.CreateMonitorCommand());
            adaptationCommand.AddCommand(triggerHandler.CreateTriggerCommand());
            adaptationCommand.AddCommand(learningHandler.CreateLearningCommand());
            adaptationCommand.AddCommand(effectivenessHandler.CreateEffectivenessCommand());
            adaptationCommand.AddCommand(dashboardHandler.CreateDashboardCommand());
            adaptationCommand.AddCommand(environmentHandler.CreateEnvironmentCommand());
            adaptationCommand.AddCommand(engineHandler.CreateStartCommand());
            adaptationCommand.AddCommand(engineHandler.CreateStopCommand());
            
            return adaptationCommand;
        }
    }
}
