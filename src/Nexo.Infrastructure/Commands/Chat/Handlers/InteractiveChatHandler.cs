using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Commands.Chat.Models;
using Nexo.Infrastructure.Commands.Chat.Utilities;
using Nexo.Infrastructure.Services.AI;
using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Chat.Handlers
{
    /// <summary>
    /// Handles interactive chat session logic
    /// </summary>
    public class InteractiveChatHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public InteractiveChatHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Starts an interactive AI chat session
        /// </summary>
        public async Task StartInteractiveChatAsync(string model, string context, double temperature, int maxTokens)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Nexo AI Chat").Color(Color.Blue));
            AnsiConsole.MarkupLine($"[bold]AI Interactive AI Chat with Offline LLama Support[/]");
            AnsiConsole.MarkupLine($"[dim]Model: {model} | Temperature: {temperature:F1} | Max Tokens: {maxTokens}[/]");
            
            if (!string.IsNullOrEmpty(context))
            {
                AnsiConsole.MarkupLine($"[dim]Context: {context}[/]");
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Type 'exit' to quit, 'help' for commands, 'clear' to clear history[/]");
            AnsiConsole.WriteLine();

            var chatHistory = new List<ChatMessage>();
            var modelSelector = new ModelSelector(_serviceProvider, _logger);
            var selectedModel = await modelSelector.SelectModelAsync(model);
            
            if (selectedModel == null)
            {
                AnsiConsole.MarkupLine("[red]ERROR: No suitable model found. Please check your model configuration.[/]");
                return;
            }

            if (!string.IsNullOrEmpty(context))
            {
                chatHistory.Add(new ChatMessage
                {
                    Role = "system",
                    Content = $"Context: {context}"
                });
            }

            var chatSessionHandler = new ChatSessionHandler(_serviceProvider, _logger, selectedModel);
            await chatSessionHandler.RunChatSessionAsync(chatHistory, temperature, maxTokens);
        }
    }
}
