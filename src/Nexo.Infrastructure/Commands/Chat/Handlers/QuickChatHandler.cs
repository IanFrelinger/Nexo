using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Commands.Chat.Utilities;
using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Chat.Handlers
{
    /// <summary>
    /// Handles quick chat processing
    /// </summary>
    public class QuickChatHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public QuickChatHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Processes a quick chat request
        /// </summary>
        public async Task ProcessQuickChatAsync(string prompt, string model, double temperature)
        {
            var modelSelector = new ModelSelector(_serviceProvider, _logger);
            var selectedModel = await modelSelector.SelectModelAsync(model);
            if (selectedModel == null)
            {
                AnsiConsole.MarkupLine("[red]ERROR: No suitable model found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[bold]AI Quick AI Response[/]");
            AnsiConsole.MarkupLine($"[dim]Model: {selectedModel.Name} | Temperature: {temperature:F1}[/]");
            AnsiConsole.WriteLine();

            try
            {
                var request = new ModelRequest
                {
                    Input = prompt,
                    Temperature = temperature,
                    MaxTokens = 2048,
                    Context = new Dictionary<string, object>
                    {
                        ["model"] = selectedModel.Name
                    }
                };

                var response = await selectedModel.ProcessAsync(request);
                
                var codeHighlighter = new CodeHighlighter();
                var highlightedResponse = codeHighlighter.HighlightCodeBlocks(response.Response);
                AnsiConsole.MarkupLine(highlightedResponse);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]ERROR: Error: {ex.Message}[/]");
                _logger.LogError(ex, "Error processing quick chat");
            }
        }
    }
}
