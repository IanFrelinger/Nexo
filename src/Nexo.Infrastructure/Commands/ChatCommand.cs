using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Infrastructure.Services.AI;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace Nexo.Infrastructure.Commands
{
    /// <summary>
    /// Interactive chat command for offline LLama AI integration
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

            // Interactive chat
            chatCommand.AddCommand(CreateInteractiveChatCommand());

            // Quick chat
            chatCommand.AddCommand(CreateQuickChatCommand());

            // Code chat
            chatCommand.AddCommand(CreateCodeChatCommand());

            return chatCommand;
        }

        /// <summary>
        /// Creates interactive chat command
        /// </summary>
        private Command CreateInteractiveChatCommand()
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
                    await StartInteractiveChatAsync(model, context, temperature, maxTokens);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to start interactive chat: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to start interactive chat");
                }
            }, modelOption, contextOption, temperatureOption, maxTokensOption);

            return interactiveCommand;
        }

        /// <summary>
        /// Creates quick chat command
        /// </summary>
        private Command CreateQuickChatCommand()
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
                    await ProcessQuickChatAsync(prompt, model, temperature);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to process quick chat: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to process quick chat");
                }
            }, promptArgument, modelOption, temperatureOption);

            return quickCommand;
        }

        /// <summary>
        /// Creates code chat command
        /// </summary>
        private Command CreateCodeChatCommand()
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
                    await ProcessCodeChatAsync(prompt, model, file, directory);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to process code chat: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to process code chat");
                }
            }, promptArgument, modelOption, fileOption, directoryOption);

            return codeCommand;
        }

        /// <summary>
        /// Starts an interactive AI chat session
        /// </summary>
        private async Task StartInteractiveChatAsync(string model, string context, double temperature, int maxTokens)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Nexo AI Chat").Color(Color.Blue));
            AnsiConsole.MarkupLine($"[bold]🤖 Interactive AI Chat with Offline LLama Support[/]");
            AnsiConsole.MarkupLine($"[dim]Model: {model} | Temperature: {temperature:F1} | Max Tokens: {maxTokens}[/]");
            
            if (!string.IsNullOrEmpty(context))
            {
                AnsiConsole.MarkupLine($"[dim]Context: {context}[/]");
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Type 'exit' to quit, 'help' for commands, 'clear' to clear history[/]");
            AnsiConsole.WriteLine();

            var chatHistory = new List<ChatMessage>();
            var selectedModel = await SelectModelAsync(model);
            
            if (selectedModel == null)
            {
                AnsiConsole.MarkupLine("[red]❌ No suitable model found. Please check your model configuration.[/]");
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

            while (true)
            {
                var input = AnsiConsole.Ask<string>("[bold blue]You:[/] ");
                
                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.ToLower() == "exit")
                    break;

                if (input.ToLower() == "help")
                {
                    ShowChatHelp();
                    continue;
                }

                if (input.ToLower() == "clear")
                {
                    chatHistory.Clear();
                    AnsiConsole.MarkupLine("[green]✅ Chat history cleared.[/]");
                    continue;
                }

                if (input.StartsWith("/"))
                {
                    await ProcessChatCommandAsync(input, selectedModel, chatHistory);
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
                    var response = await ProcessChatMessageAsync(chatHistory, selectedModel, temperature, maxTokens);
                    
                    // Apply syntax highlighting to code blocks
                    var highlightedResponse = HighlightCodeBlocks(response);
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
                    AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                    _logger.LogError(ex, "Error processing chat message");
                }
            }

            AnsiConsole.MarkupLine("[green]👋 Chat session ended.[/]");
        }

        /// <summary>
        /// Processes a quick chat request
        /// </summary>
        private async Task ProcessQuickChatAsync(string prompt, string model, double temperature)
        {
            var selectedModel = await SelectModelAsync(model);
            if (selectedModel == null)
            {
                AnsiConsole.MarkupLine("[red]❌ No suitable model found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[bold]🤖 Quick AI Response[/]");
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
                
                var highlightedResponse = HighlightCodeBlocks(response.Response);
                AnsiConsole.MarkupLine(highlightedResponse);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                _logger.LogError(ex, "Error processing quick chat");
            }
        }

        /// <summary>
        /// Processes a code chat request
        /// </summary>
        private async Task ProcessCodeChatAsync(string prompt, string model, string file, string directory)
        {
            var selectedModel = await SelectModelAsync(model, preferCodeModels: true);
            if (selectedModel == null)
            {
                AnsiConsole.MarkupLine("[red]❌ No suitable code model found.[/]");
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

            AnsiConsole.MarkupLine($"[bold]🔧 Code AI Assistant[/]");
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
                
                var highlightedResponse = HighlightCodeBlocks(response.Response);
                AnsiConsole.MarkupLine(highlightedResponse);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                _logger.LogError(ex, "Error processing code chat");
            }
        }

        /// <summary>
        /// Processes a chat message and returns AI response
        /// </summary>
        private async Task<string> ProcessChatMessageAsync(List<ChatMessage> chatHistory, IModel model, double temperature, int maxTokens)
        {
            var request = new ModelRequest
            {
                Input = chatHistory.Last(m => m.Role == "user").Content,
                SystemPrompt = chatHistory.FirstOrDefault(m => m.Role == "system")?.Content,
                Temperature = temperature,
                MaxTokens = maxTokens,
                Context = new Dictionary<string, object>
                {
                    ["model"] = model.Name
                }
            };

            var response = await model.ProcessAsync(request);
            return response.Response;
        }

        /// <summary>
        /// Processes chat commands (commands starting with /)
        /// </summary>
        private async Task ProcessChatCommandAsync(string command, IModel model, List<ChatMessage> chatHistory)
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLower();

            switch (cmd)
            {
                case "/model":
                    if (parts.Length > 1)
                    {
                        var newModel = await SelectModelAsync(parts[1]);
                        if (newModel != null)
                        {
                            AnsiConsole.MarkupLine($"[green]✅ Switched to model: {newModel.Name}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]❌ Model not found: {parts[1]}[/]");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[blue]Current model: {model.Name}[/]");
                    }
                    break;

                case "/context":
                    if (parts.Length > 1)
                    {
                        var context = string.Join(" ", parts.Skip(1));
                        chatHistory.Add(new ChatMessage
                        {
                            Role = "system",
                            Content = $"Context: {context}"
                        });
                        AnsiConsole.MarkupLine($"[green]✅ Context updated: {context}[/]");
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
                    break;

                case "/history":
                    AnsiConsole.MarkupLine($"[blue]Chat history ({chatHistory.Count} messages):[/]");
                    for (int i = 0; i < chatHistory.Count; i++)
                    {
                        var msg = chatHistory[i];
                        var role = msg.Role == "user" ? "[blue]You[/]" : msg.Role == "assistant" ? "[green]AI[/]" : "[yellow]System[/]";
                        var content = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                        AnsiConsole.MarkupLine($"{i + 1}. {role}: {content}");
                    }
                    break;

                case "/stats":
                    var stats = await GetModelStatsAsync(model);
                    AnsiConsole.MarkupLine($"[blue]Model Statistics:[/]");
                    AnsiConsole.MarkupLine($"  Name: {stats.Name}");
                    AnsiConsole.MarkupLine($"  Provider: {stats.Name}");
                    AnsiConsole.MarkupLine($"  Type: {stats.ModelType}");
                    AnsiConsole.MarkupLine($"  Max Context: {stats.MaxContextLength}");
                    AnsiConsole.MarkupLine($"  Capabilities: {string.Join(", ", GetCapabilityNames(stats.Capabilities))}");
                    break;

                default:
                    AnsiConsole.MarkupLine($"[red]❌ Unknown command: {cmd}[/]");
                    ShowChatHelp();
                    break;
            }
        }

        /// <summary>
        /// Selects the best model for the request
        /// </summary>
        private async Task<IModel?> SelectModelAsync(string modelPreference, bool preferCodeModels = false)
        {
            try
            {
                var providers = _serviceProvider.GetServices<ILlamaProvider>();
                var llamaProviders = providers.OfType<ILlamaProvider>().OrderByDescending(p => p.Priority);

                if (modelPreference == "auto")
                {
                    // Select the highest priority available provider
                    foreach (var provider in llamaProviders)
                    {
                        if (provider.IsOfflineCapable)
                        {
                            var models = await provider.GetAvailableModelsAsync();
                            var selectedModel = models.FirstOrDefault();

                            if (selectedModel != null)
                            {
                                await provider.LoadModelAsync(selectedModel.Name);
                                return new LlamaNativeModel(selectedModel.Name, _logger, (LlamaNativeProvider)provider);
                            }
                        }
                    }
                }
                else
                {
                    // Try to find specific model
                    foreach (var provider in llamaProviders)
                    {
                        var models = await provider.GetAvailableModelsAsync();
                        var model = models.FirstOrDefault(m => m.Name.Contains(modelPreference, StringComparison.OrdinalIgnoreCase));
                        
                        if (model != null)
                        {
                            await provider.LoadModelAsync(model.Name);
                            return new LlamaNativeModel(model.Name, _logger, (LlamaNativeProvider)provider);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting model");
                return null;
            }
        }

        /// <summary>
        /// Highlights code blocks in the response
        /// </summary>
        private static string HighlightCodeBlocks(string text)
        {
            // Simple code block highlighting using Spectre.Console markup
            var codeBlockPattern = @"```(\w+)?\n(.*?)```";
            var matches = Regex.Matches(text, codeBlockPattern, RegexOptions.Singleline);

            foreach (Match match in matches.Cast<Match>().Reverse())
            {
                var language = match.Groups[1].Value;
                var code = match.Groups[2].Value.Trim();
                
                var highlightedCode = $"[dim]```{language}[/]\n[bold]{code}[/]\n[dim]```[/]";
                text = text.Substring(0, match.Index) + highlightedCode + text.Substring(match.Index + match.Length);
            }

            return text;
        }

        /// <summary>
        /// Shows chat help information
        /// </summary>
        private static void ShowChatHelp()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]🤖 Chat Commands:[/]");
            AnsiConsole.MarkupLine("  [blue]exit[/]     - Exit the chat session");
            AnsiConsole.MarkupLine("  [blue]help[/]     - Show this help message");
            AnsiConsole.MarkupLine("  [blue]clear[/]    - Clear chat history");
            AnsiConsole.MarkupLine("  [blue]/model <name>[/] - Switch to specific model");
            AnsiConsole.MarkupLine("  [blue]/context <text>[/] - Set context for the session");
            AnsiConsole.MarkupLine("  [blue]/history[/] - Show chat history");
            AnsiConsole.MarkupLine("  [blue]/stats[/]   - Show model statistics");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]💡 Tips:[/]");
            AnsiConsole.MarkupLine("  - Be specific in your questions");
            AnsiConsole.MarkupLine("  - Use code blocks for code examples");
            AnsiConsole.MarkupLine("  - Ask follow-up questions for clarification");
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Gets model statistics
        /// </summary>
        private async Task<ModelInfo> GetModelStatsAsync(IModel model)
        {
            await Task.CompletedTask;
            return new ModelInfo
            {
                Name = model.Name,
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

        /// <summary>
        /// Chat message model
        /// </summary>
        private class ChatMessage
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }
    }
}
