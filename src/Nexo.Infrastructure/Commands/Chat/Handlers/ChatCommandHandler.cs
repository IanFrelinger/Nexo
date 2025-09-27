using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Commands.Chat.Models;
using Nexo.Infrastructure.Commands.Chat.Utilities;
using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Chat.Handlers
{
    /// <summary>
    /// Handles chat commands (commands starting with /)
    /// </summary>
    public partial class ChatCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModel _model;

        public ChatCommandHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModel model)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// Processes chat commands (commands starting with /)
        /// </summary>
        public async Task ProcessChatCommandAsync(string command, List<ChatMessage> chatHistory)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLower();

            switch (cmd)
            {
                case "/model":
                    await HandleModelCommandAsync(parts, chatHistory);
                    break;

                case "/context":
                    HandleContextCommand(parts, chatHistory);
                    break;

                case "/history":
                    HandleHistoryCommand(chatHistory);
                    break;

                case "/stats":
                    await HandleStatsCommandAsync();
                    break;

                default:
                    AnsiConsole.MarkupLine($"[red]ERROR: Unknown command: {cmd}[/]");
                    var helpHandler = new HelpHandler();
                    helpHandler.ShowChatHelp();
                    break;
            }
        }

        private async Task HandleModelCommandAsync(string[] parts, List<ChatMessage> chatHistory)
        {
            if (parts.Length > 1)
            {
                var modelSelector = new ModelSelector(_serviceProvider, _logger);
                var newModel = await modelSelector.SelectModelAsync(parts[1]);
                if (newModel != null)
                {
                    AnsiConsole.MarkupLine($"[green]SUCCESS: Switched to model: {newModel.Name}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]ERROR: Model not found: {parts[1]}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[blue]Current model: {_model.Name}[/]");
            }
        }

        private void HandleContextCommand(string[] parts, List<ChatMessage> chatHistory)
        {
            if (parts.Length > 1)
            {
                var context = string.Join(" ", parts.Skip(1));
                chatHistory.Add(new ChatMessage
                {
                    Role = "system",
                    Content = $"Context: {context}"
                });
                AnsiConsole.MarkupLine($"[green]SUCCESS: Context updated: {context}[/]");
            }
            else
            {
                var contextMsg = chatHistory.FirstOrDefault(m => m.Role == "system");
                if (contextMsg != null)
                {
                    AnsiConsole.MarkupLine($"[blue]Current context: {contextMsg.Content}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No context set[/]");
                }
            }
        }

        private void HandleHistoryCommand(List<ChatMessage> chatHistory)
        {
            AnsiConsole.MarkupLine($"[blue]Chat history ({chatHistory.Count} messages):[/]");
            for (int i = 0; i < chatHistory.Count; i++)
            {
                var msg = chatHistory[i];
                var role = msg.Role == "user" ? "[blue]You[/]" : msg.Role == "assistant" ? "[green]AI[/]" : "[yellow]System[/]";
                var content = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                AnsiConsole.MarkupLine($"{i + 1}. {role}: {content}");
            }
        }

        private async Task HandleStatsCommandAsync()
        {
            var stats = await GetModelStatsAsync();
            AnsiConsole.MarkupLine($"[blue]Model Statistics:[/]");
            AnsiConsole.MarkupLine($"  Name: {stats.Name}");
            AnsiConsole.MarkupLine($"  Provider: {stats.Name}");
            AnsiConsole.MarkupLine($"  Type: {stats.ModelType}");
            AnsiConsole.MarkupLine($"  Max Context: {stats.MaxContextLength}");
            AnsiConsole.MarkupLine($"  Capabilities: {string.Join(", ", GetCapabilityNames(stats.Capabilities))}");
        }

        /// <summary>
        /// Gets model statistics
        /// </summary>
        private async Task<ModelInfo> GetModelStatsAsync()
        {
            await Task.CompletedTask;
            return new ModelInfo
            {
                Name = _model.Name,
                ModelType = Nexo.Feature.AI.Enums.ModelType.TextGeneration
            };
        }

        /// <summary>
        /// Gets capability names for display
        /// </summary>
        private static List<string> GetCapabilityNames(ModelCapabilities capabilities)
        {
            var names = new List<string>();
            if (capabilities.SupportsTextGeneration) names.Add("Text");
            if (capabilities.SupportsCodeGeneration) names.Add("Code");
            if (capabilities.SupportsAnalysis) names.Add("Analysis");
            if (capabilities.SupportsOptimization) names.Add("Optimization");
            if (capabilities.SupportsStreaming) names.Add("Streaming");
            // Chat support not available in this ModelCapabilities
            return names;
        }
    }
}
