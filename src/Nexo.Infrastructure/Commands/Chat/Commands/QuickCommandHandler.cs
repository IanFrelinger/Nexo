using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Infrastructure.Commands.Chat.Handlers;
using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Chat.Commands
{
    /// <summary>
    /// Handles quick chat command creation and execution
    /// </summary>
    public partial class QuickCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public QuickCommandHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        public Command CreateQuickCommand()
        {
            var quickCommand = new Command("quick", "Quick AI chat without interactive mode");
            var promptArgument = new Argument<string>("prompt", "The prompt to send to AI");
            var modelOption = new Option<string>("--model", () => "auto", "AI model to use");
            var temperatureOption = new Option<double>("--temperature", () => 0.7, "Temperature for response generation");

            quickCommand.AddArgument(promptArgument);
            quickCommand.AddOption(modelOption);
            quickCommand.AddOption(temperatureOption);

            quickCommand.SetHandler(async (string prompt, string model, double temperature) =>
            {
                try
                {
                    var quickChatHandler = new QuickChatHandler(_serviceProvider, _logger, _modelOrchestrator);
                    await quickChatHandler.ProcessQuickChatAsync(prompt, model, temperature);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]ERROR: Failed to process quick chat: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to process quick chat");
                }
            }, promptArgument, modelOption, temperatureOption);

            return quickCommand;
        }
    }
}
