using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Infrastructure.Commands.Chat.Handlers;
using Nexo.Infrastructure.Commands.Chat.Models;
using Nexo.Infrastructure.Commands.Chat.Utilities;
using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Chat.Commands
{
    /// <summary>
    /// Handles interactive chat command creation and execution
    /// </summary>
    public partial class InteractiveCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public InteractiveCommandHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        public Command CreateInteractiveCommand()
        {
            var interactiveCommand = new Command("interactive", "Start interactive AI chat session");
            var modelOption = new Option<string>("--model", () => "auto", "AI model to use (auto, ollama, native, or specific model name)");
            var contextOption = new Option<string>("--context", "Additional context for the chat session");
            var temperatureOption = new Option<double>("--temperature", () => 0.7, "Temperature for response generation (0.0-1.0)");
            var maxTokensOption = new Option<int>("--max-tokens", () => 2048, "Maximum tokens in response");

            interactiveCommand.AddOption(modelOption);
            interactiveCommand.AddOption(contextOption);
            interactiveCommand.AddOption(temperatureOption);
            interactiveCommand.AddOption(maxTokensOption);

            interactiveCommand.SetHandler(async (string model, string context, double temperature, int maxTokens) =>
            {
                try
                {
                    var interactiveChatHandler = new InteractiveChatHandler(_serviceProvider, _logger, _modelOrchestrator);
                    await interactiveChatHandler.StartInteractiveChatAsync(model, context, temperature, maxTokens);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]ERROR: Failed to start interactive chat: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to start interactive chat");
                }
            }, modelOption, contextOption, temperatureOption, maxTokensOption);

            return interactiveCommand;
        }
    }
}
