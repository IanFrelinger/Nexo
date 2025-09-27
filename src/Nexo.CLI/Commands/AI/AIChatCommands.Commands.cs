using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.AI
{
    /// <summary>
    /// Command creation functionality
    /// </summary>
    public partial class AIChatCommands
    {
        /// <summary>
        /// Creates interactive chat command.
        /// </summary>
        private Command CreateInteractiveChatCommand()
        {
            var interactiveCommand = new Command("interactive", "Start interactive AI chat session");
            var modelOption = new Option<string>("--model", () => "gpt-4", "AI model to use for chat");
            var contextOption = new Option<string>("--context", "Additional context for the chat session");
            var temperatureOption = new Option<double>("--temperature", () => 0.7, "Temperature for response generation (0.0-1.0)");

            interactiveCommand.AddOption(modelOption);
            interactiveCommand.AddOption(contextOption);
            interactiveCommand.AddOption(temperatureOption);

            interactiveCommand.SetHandler(async (string model, string context, double temperature) =>
            {
                try
                {
                    await StartInteractiveChatAsync(model, context, temperature);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to start interactive chat: {ex.Message}");
                    _logger.LogError(ex, "Failed to start interactive chat");
                }
            }, modelOption, contextOption, temperatureOption);

            return interactiveCommand;
        }

        /// <summary>
        /// Creates code review chat command.
        /// </summary>
        private Command CreateCodeReviewCommand()
        {
            var reviewCommand = new Command("review", "AI-powered code review chat");
            var fileOption = new Option<string>("--file", "File to review");
            var directoryOption = new Option<string>("--directory", "Directory to review");
            var focusOption = new Option<string>("--focus", "Focus area (security, performance, style, etc.)");

            reviewCommand.AddOption(fileOption);
            reviewCommand.AddOption(directoryOption);
            reviewCommand.AddOption(focusOption);

            reviewCommand.SetHandler(async (string file, string directory, string focus) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(file) && string.IsNullOrEmpty(directory))
                    {
                        Console.WriteLine("ERROR: Please specify either --file or --directory");
                        return;
                    }

                    await StartCodeReviewChatAsync(file, directory, focus);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to start code review chat: {ex.Message}");
                    _logger.LogError(ex, "Failed to start code review chat");
                }
            }, fileOption, directoryOption, focusOption);

            return reviewCommand;
        }

        /// <summary>
        /// Creates architecture chat command.
        /// </summary>
        private Command CreateArchitectureChatCommand()
        {
            var archCommand = new Command("architecture", "AI-powered architecture discussion");
            var projectOption = new Option<string>("--project", "Project path to analyze");
            var topicOption = new Option<string>("--topic", "Architecture topic to discuss");

            archCommand.AddOption(projectOption);
            archCommand.AddOption(topicOption);

            archCommand.SetHandler(async (string project, string topic) =>
            {
                try
                {
                    await StartArchitectureChatAsync(project, topic);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to start architecture chat: {ex.Message}");
                    _logger.LogError(ex, "Failed to start architecture chat");
                }
            }, projectOption, topicOption);

            return archCommand;
        }

        /// <summary>
        /// Creates debugging chat command.
        /// </summary>
        private Command CreateDebuggingChatCommand()
        {
            var debugCommand = new Command("debug", "AI-powered debugging assistance");
            var errorOption = new Option<string>("--error", "Error message or stack trace");
            var logOption = new Option<string>("--log", "Log file to analyze");
            var contextOption = new Option<string>("--context", "Additional debugging context");

            debugCommand.AddOption(errorOption);
            debugCommand.AddOption(logOption);
            debugCommand.AddOption(contextOption);

            debugCommand.SetHandler(async (string error, string log, string context) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(error) && string.IsNullOrEmpty(log))
                    {
                        Console.WriteLine("ERROR: Please specify either --error or --log");
                        return;
                    }

                    await StartDebuggingChatAsync(error, log, context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to start debugging chat: {ex.Message}");
                    _logger.LogError(ex, "Failed to start debugging chat");
                }
            }, errorOption, logOption, contextOption);

            return debugCommand;
        }

        /// <summary>
        /// Creates documentation chat command.
        /// </summary>
        private Command CreateDocumentationChatCommand()
        {
            var docCommand = new Command("docs", "AI-powered documentation assistance");
            var typeOption = new Option<string>("--type", "Documentation type (api, readme, comments, etc.)");
            var targetOption = new Option<string>("--target", "Target file or directory for documentation");

            docCommand.AddOption(typeOption);
            docCommand.AddOption(targetOption);

            docCommand.SetHandler(async (string type, string target) =>
            {
                try
                {
                    await StartDocumentationChatAsync(type, target);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: Failed to start documentation chat: {ex.Message}");
                    _logger.LogError(ex, "Failed to start documentation chat");
                }
            }, typeOption, targetOption);

            return docCommand;
        }
    }
}
