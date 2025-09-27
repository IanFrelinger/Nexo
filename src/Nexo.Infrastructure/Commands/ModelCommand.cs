using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Spectre.Console;
using System.Linq;
using Nexo.Infrastructure.Commands.Model;

namespace Nexo.Infrastructure.Commands
{
    /// <summary>
    /// Orchestrator for model management commands that delegates to specialized command handlers.
    /// </summary>
    public partial class ModelCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModelCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly IEnumerable<IAIProvider> _providers;
        private readonly ModelListHandler _listHandler;
        private readonly ModelPullHandler _pullHandler;
        private readonly ModelRemoveHandler _removeHandler;
        private readonly ModelInfoHandler _infoHandler;
        private readonly ModelHealthHandler _healthHandler;

        public ModelCommand(
            IServiceProvider serviceProvider, 
            ILogger<ModelCommand> logger, 
            IModelOrchestrator modelOrchestrator, 
            IEnumerable<IAIProvider> providers)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            
            _listHandler = new ModelListHandler(_modelOrchestrator, _providers, _logger);
            _pullHandler = new ModelPullHandler(_modelOrchestrator, _logger);
            _removeHandler = new ModelRemoveHandler(_modelOrchestrator, _logger);
            _infoHandler = new ModelInfoHandler(_modelOrchestrator, _logger);
            _healthHandler = new ModelHealthHandler(_modelOrchestrator, _providers, _logger);
        }

        /// <summary>
        /// Creates the model command with all subcommands
        /// </summary>
        public Command CreateModelCommand()
        {
            var modelCommand = new Command("model", "Manage AI models for offline LLama integration");

            // List models
            modelCommand.AddCommand(_listHandler.CreateListCommand());

            // Pull/download model
            modelCommand.AddCommand(_pullHandler.CreatePullCommand());

            // Remove model
            modelCommand.AddCommand(_removeHandler.CreateRemoveCommand());

            // Model info
            modelCommand.AddCommand(_infoHandler.CreateInfoCommand());

            // Health check
            modelCommand.AddCommand(_healthHandler.CreateHealthCommand());

            return modelCommand;
        }
    }
}
