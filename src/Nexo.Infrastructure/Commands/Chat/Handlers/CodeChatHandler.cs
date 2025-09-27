using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Handles code chat processing
    /// </summary>
    public class CodeChatHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public CodeChatHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModelOrchestrator modelOrchestrator)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Processes a code chat request
        /// </summary>
        public async Task ProcessCodeChatAsync(string prompt, string model, string file, string directory)
        {
            var modelSelector = new ModelSelector(_serviceProvider, _logger);
            var selectedModel = await modelSelector.SelectModelAsync(model, preferCodeModels: true);
            if (selectedModel == null)
            {
                AnsiConsole.MarkupLine("[red]ERROR: No suitable code model found.[/]");
                return;
            }

            var systemPrompt = "You are an expert software developer and code reviewer. Provide detailed, accurate, and helpful responses about code, programming concepts, and software development best practices.";
            var contextPrompt = prompt;

            // Add file context if provided
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                var fileContent = await File.ReadAllTextAsync(file);
                contextPrompt = $"File: {file}\n\n{fileContent}\n\nQuestion: {prompt}";
            }

            // Add directory context if provided
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories).Take(5);
                var directoryContext = string.Join("\n", files.Select(f => $"File: {f}\n{File.ReadAllText(f).Substring(0, Math.Min(500, File.ReadAllText(f).Length))}..."));
                contextPrompt = $"Directory: {directory}\n\n{directoryContext}\n\nQuestion: {prompt}";
            }

            AnsiConsole.MarkupLine($"[bold]Tool Code AI Assistant[/]");
            AnsiConsole.MarkupLine($"[dim]Model: {selectedModel.Name}[/]");
            AnsiConsole.WriteLine();

            try
            {
                var request = new ModelRequest
                {
                    Input = contextPrompt,
                    SystemPrompt = systemPrompt,
                    Temperature = 0.3, // Lower temperature for code
                    MaxTokens = 4096,
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
                _logger.LogError(ex, "Error processing code chat");
            }
        }
    }
}
