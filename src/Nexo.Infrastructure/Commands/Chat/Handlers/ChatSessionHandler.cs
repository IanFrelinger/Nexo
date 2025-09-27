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
    /// Handles chat session management and interaction
    /// </summary>
    public partial class ChatSessionHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;
        private readonly IModel _model;

        public ChatSessionHandler(IServiceProvider serviceProvider, ILogger<ChatCommand> logger, IModel model)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>
        /// Runs the main chat session loop
        /// </summary>
        public async Task RunChatSessionAsync(List<ChatMessage> chatHistory, double temperature, int maxTokens)
        {
            while (true)
            {
                var input = AnsiConsole.Ask<string>("[bold blue]You:[/] ");
                
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "exit")
                    break;

                if (input.ToLower() == "help")
                {
                    var helpHandler = new HelpHandler();
                    helpHandler.ShowChatHelp();
                    continue;
                }

                if (input.ToLower() == "clear")
                {
                    chatHistory.Clear();
                    AnsiConsole.MarkupLine("[green]SUCCESS: Chat history cleared.[/]");
                    continue;
                }

                if (input.StartsWith("/"))
                {
                    var commandHandler = new ChatCommandHandler(_serviceProvider, _logger, _model);
                    await commandHandler.ProcessChatCommandAsync(input, chatHistory);
                    continue;
                }

                try
                {
                    var userMessage = new ChatMessage
                    {
                        Role = "user",
                        Content = input
                    };
                    chatHistory.Add(userMessage);

                    AnsiConsole.Write("[bold green]AI:[/] ");
                    var response = await ProcessChatMessageAsync(chatHistory, temperature, maxTokens);
                    
                    // Apply syntax highlighting to code blocks
                    var codeHighlighter = new CodeHighlighter();
                    var highlightedResponse = codeHighlighter.HighlightCodeBlocks(response);
                    AnsiConsole.MarkupLine(highlightedResponse);
                    AnsiConsole.WriteLine();

                    chatHistory.Add(new ChatMessage
                    {
                        Role = "assistant",
                        Content = response
                    });
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]ERROR: Error: {ex.Message}[/]");
                    _logger.LogError(ex, "Error processing chat message");
                }
            }

            AnsiConsole.MarkupLine("[green]Goodbye Chat session ended.[/]");
        }

        /// <summary>
        /// Processes a chat message and returns AI response
        /// </summary>
        private async Task<string> ProcessChatMessageAsync(List<ChatMessage> chatHistory, double temperature, int maxTokens)
        {
            var request = new ModelRequest
            {
                Input = chatHistory.Last(m => m.Role == "user").Content,
                SystemPrompt = chatHistory.FirstOrDefault(m => m.Role == "system")?.Content,
                Temperature = temperature,
                MaxTokens = maxTokens,
                Context = new Dictionary<string, object>
                {
                    ["model"] = _model.Name
                }
            };

            var response = await _model.ProcessAsync(request);
            return response.Response;
        }
    }
}
