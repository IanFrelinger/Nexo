using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.CLI.Commands;
using Nexo.CLI.Commands.AI;
using Nexo.CLI.Commands.Unity;
using Nexo.CLI.Interactive;
using Nexo.CLI.Dashboard;
using Nexo.CLI.Help;
using Nexo.CLI.Progress;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Template.Interfaces;
using Nexo.Shared.Interfaces;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Central command aggregator that composes all commands from the project
    /// and provides a unified entry point for execution
    /// </summary>
    public partial class CentralCommandAggregator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CentralCommandAggregator> _logger;
        private readonly Dictionary<string, CommandCategory> _commandCategories;

        public CentralCommandAggregator(IServiceProvider serviceProvider, ILogger<CentralCommandAggregator> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandCategories = new Dictionary<string, CommandCategory>();
        }

        /// <summary>
        /// Creates the central command aggregator with all available commands
        /// </summary>
        public Command CreateCentralCommand()
        {
            var rootCommand = new Command("nexo", "Nexo Central Command Aggregator - Unified access to all project commands");

            // Initialize command categories
            InitializeCommandCategories();

            // Add all command categories
            foreach (var category in _commandCategories.Values)
            {
                rootCommand.AddCommand(category.RootCommand);
            }

            // Add central commands
            rootCommand.AddCommand(CreateOrchestrationCommand());
            rootCommand.AddCommand(CreateDiscoveryCommand());
            rootCommand.AddCommand(CreateExecutionCommand());

            // Add demo commands (consolidated)
            rootCommand.AddCommand(CreateDemoCommands());

            return rootCommand;
        }
    }
}
