using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Infrastructure.Commands.Chat.Commands;
using Nexo.Infrastructure.Commands.Chat.Handlers;
using Nexo.Infrastructure.Commands.Chat.Models;

namespace Nexo.Infrastructure.Commands.Chat.Core
{
    /// <summary>
    /// Main orchestrator for chat command functionality
    /// </summary>
    public class ChatCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public ChatCommand(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Creates the chat command with all subcommands
        /// </summary>
        public Command CreateChatCommand()
        {
            var chatCommand = new Command("chat", "Interactive AI chat interface with offline LLama support");

            // Create command handlers
            var interactiveCommandHandler = new InteractiveCommandHandler(_serviceProvider, _logger, _modelOrchestrator);
            var quickCommandHandler = new QuickCommandHandler(_serviceProvider, _logger, _modelOrchestrator);
            var codeCommandHandler = new CodeCommandHandler(_serviceProvider, _logger, _modelOrchestrator);

            // Add subcommands
            chatCommand.AddCommand(interactiveCommandHandler.CreateInteractiveCommand());
            chatCommand.AddCommand(quickCommandHandler.CreateQuickCommand());
            chatCommand.AddCommand(codeCommandHandler.CreateCodeCommand());

            return chatCommand;
        }
    }
}
