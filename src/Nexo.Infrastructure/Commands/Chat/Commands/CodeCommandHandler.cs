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
    /// Handles code chat command creation and execution
    /// </summary>
    public partial class CodeCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public CodeCommandHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        public Command CreateCodeCommand()
        {
            var codeCommand = new Command("code", "AI chat specialized for code-related questions");
            var promptArgument = new Argument<string>("prompt", "The code-related prompt");
            var modelOption = new Option<string>("--model", () => "auto", "AI model to use (prefers code models)");
            var fileOption = new Option<string>("--file", "Include file content in context");
            var directoryOption = new Option<string>("--directory", "Include directory context");

            codeCommand.AddArgument(promptArgument);
            codeCommand.AddOption(modelOption);
            codeCommand.AddOption(fileOption);
            codeCommand.AddOption(directoryOption);

            codeCommand.SetHandler(async (string prompt, string model, string file, string directory) =>
            {
                try
                {
                    var codeChatHandler = new CodeChatHandler(_serviceProvider, _logger, _modelOrchestrator);
                    await codeChatHandler.ProcessCodeChatAsync(prompt, model, file, directory);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]ERROR: Failed to process code chat: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to process code chat");
                }
            }, promptArgument, modelOption, fileOption, directoryOption);

            return codeCommand;
        }
    }
}
