using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.CLI.Dashboard;
using Nexo.CLI.Progress;
using Nexo.CLI.Help;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Interactive CLI framework with intelligent suggestions and guided workflows
    /// </summary>
    public class InteractiveCLI : IInteractiveCLI
    {
        private readonly ICommandSuggestionEngine _suggestionEngine;
        private readonly ICLIStateManager _stateManager;
        private readonly IRealTimeDashboard _dashboard;
        private readonly ILogger<InteractiveCLI> _logger;
        private readonly IModelOrchestrator _aiOrchestrator;
        
        public InteractiveCLI(
            ICommandSuggestionEngine suggestionEngine,
            ICLIStateManager stateManager,
            IRealTimeDashboard dashboard,
            ILogger<InteractiveCLI> logger,
            IModelOrchestrator aiOrchestrator)
        {
            _suggestionEngine = suggestionEngine;
            _stateManager = stateManager;
            _dashboard = dashboard;
            _logger = logger;
            _aiOrchestrator = aiOrchestrator;
        }
        
        public async Task StartInteractiveModeAsync()
        {
            Console.WriteLine("🚀 Welcome to Nexo Interactive Mode");
            Console.WriteLine("Type 'help' for available commands or 'exit' to quit");
            Console.WriteLine("Use Tab for auto-completion and Ctrl+R for command history");
            Console.WriteLine();
            
            await InitializeInteractiveEnvironment();
            
            while (true)
            {
                try
                {
                    var prompt = await GenerateSmartPrompt();
                    var input = await ReadInteractiveInput(prompt);
                    
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    
                    if (input.ToLower() == "exit") break;
                    
                    await ProcessInteractiveCommand(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    _logger.LogError(ex, "Interactive CLI error");
                }
            }
            
            Console.WriteLine("👋 Goodbye!");
        }
        
        public async Task ProcessInteractiveCommandAsync(string command)
        {
            await ProcessInteractiveCommand(command);
        }
        
        public async Task ShowContextualHelpAsync()
        {
            var context = await _stateManager.GetCurrentContextAsync();
            var suggestions = await _suggestionEngine.GetContextualSuggestionsAsync(context);
            
            Console.WriteLine("\n🔍 Contextual Help");
            Console.WriteLine("═══════════════════");
            Console.WriteLine();
            
            if (context.CurrentProject != null)
            {
                Console.WriteLine($"📁 Current Project: {context.CurrentProject.Name} ({context.CurrentProject.Type})");
            }
            
            if (!string.IsNullOrEmpty(context.CurrentPlatform))
            {
                Console.WriteLine($"🖥️  Current Platform: {context.CurrentPlatform}");
            }
            
            Console.WriteLine();
            Console.WriteLine("💡 Suggested Commands:");
            
            foreach (var suggestion in suggestions.Take(5))
            {
                Console.WriteLine($"  • {suggestion.Command.PadRight(25)} - {suggestion.Description}");
                if (!string.IsNullOrEmpty(suggestion.Reason))
                {
                    Console.WriteLine($"    {suggestion.Reason}");
                }
            }
            
            Console.WriteLine();
        }
        
        public async Task ShowSystemStatusAsync()
        {
            var context = await _stateManager.GetCurrentContextAsync();
            
            Console.WriteLine("\n📊 System Status");
            Console.WriteLine("═══════════════════");
            Console.WriteLine();
            
            Console.WriteLine($"📁 Working Directory: {context.WorkingDirectory}");
            Console.WriteLine($"🕒 Last Activity: {context.LastActivity:yyyy-MM-dd HH:mm:ss}");
            
            if (context.CurrentProject != null)
            {
                Console.WriteLine($"📁 Current Project: {context.CurrentProject.Name}");
                Console.WriteLine($"   Type: {context.CurrentProject.Type}");
                Console.WriteLine($"   Path: {context.CurrentProject.Path}");
                Console.WriteLine($"   Last Modified: {context.CurrentProject.LastModified:yyyy-MM-dd HH:mm:ss}");
            }
            
            if (!string.IsNullOrEmpty(context.CurrentPlatform))
            {
                Console.WriteLine($"🖥️  Current Platform: {context.CurrentPlatform}");
            }
            
            Console.WriteLine($"📈 Active Monitoring: {(context.HasActiveMonitoring ? "Yes" : "No")}");
            Console.WriteLine($"🔄 Pending Adaptations: {(context.HasPendingAdaptations ? "Yes" : "No")}");
            Console.WriteLine($"⚠️  Performance Issues: {(context.HasPerformanceIssues ? "Yes" : "No")}");
            
            Console.WriteLine();
            Console.WriteLine("📋 Recent Commands:");
            foreach (var cmd in context.RecentCommands.Take(5))
            {
                Console.WriteLine($"  • {cmd}");
            }
            
            Console.WriteLine();
        }
        
        private async Task InitializeInteractiveEnvironment()
        {
            try
            {
                // Initialize any required services
                await _stateManager.SaveStateAsync();
                _logger.LogInformation("Interactive CLI environment initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize interactive environment");
                Console.WriteLine($"Warning: Failed to initialize some features: {ex.Message}");
            }
        }
        
        private async Task<string> GenerateSmartPrompt()
        {
            var context = await _stateManager.GetCurrentContextAsync();
            var suggestions = await _suggestionEngine.GetContextualSuggestionsAsync(context);
            
            var promptBuilder = new StringBuilder();
            promptBuilder.Append("nexo");
            
            // Add context indicators
            if (context.CurrentProject != null)
            {
                promptBuilder.Append($" [{context.CurrentProject.Name}]");
            }
            
            if (context.CurrentPlatform != null)
            {
                promptBuilder.Append($" ({context.CurrentPlatform})");
            }
            
            // Add status indicators
            if (context.HasActiveMonitoring)
            {
                promptBuilder.Append(" 📊");
            }
            
            if (context.HasPendingAdaptations)
            {
                promptBuilder.Append(" 🔄");
            }
            
            if (context.HasPerformanceIssues)
            {
                promptBuilder.Append(" ⚠️");
            }
            
            promptBuilder.Append("> ");
            
            return promptBuilder.ToString();
        }
        
        private async Task<string> ReadInteractiveInput(string prompt)
        {
            Console.Write(prompt);
            
            var input = new StringBuilder();
            var cursorPosition = 0;
            var historyIndex = -1;
            var commandHistory = await _stateManager.GetCommandHistoryAsync();
            
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        var command = input.ToString();
                        await _stateManager.AddToHistoryAsync(command);
                        return command;
                        
                    case ConsoleKey.Tab:
                        await HandleTabCompletion(input, cursorPosition);
                        break;
                        
                    case ConsoleKey.UpArrow:
                        if (commandHistory.Any() && historyIndex < commandHistory.Count - 1)
                        {
                            historyIndex++;
                            await ReplaceCurrentInput(input, commandHistory[historyIndex]);
                        }
                        break;
                        
                    case ConsoleKey.DownArrow:
                        if (historyIndex > 0)
                        {
                            historyIndex--;
                            await ReplaceCurrentInput(input, commandHistory[historyIndex]);
                        }
                        else if (historyIndex == 0)
                        {
                            historyIndex = -1;
                            await ReplaceCurrentInput(input, "");
                        }
                        break;
                        
                    case ConsoleKey.Backspace:
                        if (input.Length > 0 && cursorPosition > 0)
                        {
                            input.Remove(cursorPosition - 1, 1);
                            cursorPosition--;
                            await RefreshInputDisplay(input, cursorPosition);
                        }
                        break;
                        
                    case ConsoleKey.Escape:
                        return "exit";
                        
                    default:
                        if (!char.IsControl(keyInfo.KeyChar))
                        {
                            input.Insert(cursorPosition, keyInfo.KeyChar);
                            cursorPosition++;
                            await RefreshInputDisplay(input, cursorPosition);
                        }
                        break;
                }
            }
        }
        
        private async Task HandleTabCompletion(StringBuilder input, int cursorPosition)
        {
            var currentInput = input.ToString();
            var completions = await _suggestionEngine.GetCompletionsAsync(currentInput);
            
            if (completions.Count() == 1)
            {
                // Single completion - apply it
                var completion = completions.First();
                input.Clear();
                input.Append(completion);
                await RefreshInputDisplay(input, completion.Length);
            }
            else if (completions.Count() > 1)
            {
                // Multiple completions - show options
                Console.WriteLine();
                await DisplayCompletionOptions(completions);
                Console.Write(await GenerateSmartPrompt());
                Console.Write(currentInput);
            }
        }
        
        private async Task DisplayCompletionOptions(IEnumerable<string> completions)
        {
            Console.WriteLine("Available completions:");
            foreach (var completion in completions.Take(10))
            {
                Console.WriteLine($"  {completion}");
            }
            Console.WriteLine();
        }
        
        private async Task ReplaceCurrentInput(StringBuilder input, string newInput)
        {
            // Clear current line
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            
            // Set new input
            input.Clear();
            input.Append(newInput);
            
            // Display new input
            Console.Write(await GenerateSmartPrompt());
            Console.Write(newInput);
        }
        
        private async Task RefreshInputDisplay(StringBuilder input, int cursorPosition)
        {
            // Clear current line and redraw
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            Console.Write(await GenerateSmartPrompt());
            Console.Write(input.ToString());
            
            // Position cursor
            if (cursorPosition < input.Length)
            {
                Console.CursorLeft = Console.CursorLeft - (input.Length - cursorPosition);
            }
        }
        
        private async Task ProcessInteractiveCommand(string input)
        {
            var args = ParseCommandInput(input);
            
            // Check for special interactive commands
            if (await HandleSpecialCommands(args)) return;
            
            // Execute regular command with enhanced output
            await ExecuteCommandWithProgress(args);
        }
        
        private string[] ParseCommandInput(string input)
        {
            // Simple command parsing - can be enhanced
            return input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
        
        private async Task<bool> HandleSpecialCommands(string[] args)
        {
            if (args.Length == 0) return false;
            
            switch (args[0].ToLower())
            {
                case "help":
                    await ShowInteractiveHelp(args.Length > 1 ? args[1] : null);
                    return true;
                    
                case "status":
                    await ShowSystemStatusAsync();
                    return true;
                    
                case "dashboard":
                    await _dashboard.ShowRealTimeDashboard();
                    return true;
                    
                case "suggest":
                    await ShowCommandSuggestions();
                    return true;
                    
                case "history":
                    await ShowCommandHistory();
                    return true;
                    
                case "clear":
                    Console.Clear();
                    return true;
                    
                case "context":
                    await ShowContextualHelpAsync();
                    return true;
                    
                default:
                    return false;
            }
        }
        
        private async Task ShowInteractiveHelp(string? specificTopic = null)
        {
            if (specificTopic != null)
            {
                await ShowTopicHelp(specificTopic);
                return;
            }
            
            Console.WriteLine("\n🔍 Nexo Interactive Help");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            Console.WriteLine("📚 Available Commands:");
            Console.WriteLine("  help [topic]          - Show help for a specific topic");
            Console.WriteLine("  status                - Show current system status");
            Console.WriteLine("  dashboard             - Open real-time monitoring dashboard");
            Console.WriteLine("  suggest               - Show intelligent command suggestions");
            Console.WriteLine("  history               - Show command history");
            Console.WriteLine("  context               - Show contextual help");
            Console.WriteLine("  clear                 - Clear the screen");
            Console.WriteLine("  exit                  - Exit interactive mode");
            Console.WriteLine();
            
            Console.WriteLine("💡 Interactive Features:");
            Console.WriteLine("  • Tab completion for commands and parameters");
            Console.WriteLine("  • Command history with Up/Down arrows");
            Console.WriteLine("  • Intelligent suggestions based on context");
            Console.WriteLine("  • Real-time monitoring dashboard");
            Console.WriteLine("  • Progress tracking for long operations");
            Console.WriteLine();
            
            Console.WriteLine("🎯 Quick Start:");
            Console.WriteLine("  • Type 'status' to see current context");
            Console.WriteLine("  • Type 'suggest' for intelligent recommendations");
            Console.WriteLine("  • Type 'dashboard' for real-time monitoring");
            Console.WriteLine();
        }
        
        private async Task ShowTopicHelp(string topic)
        {
            Console.WriteLine($"\n📖 Help for: {topic}");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            // This would integrate with the help system
            Console.WriteLine($"Detailed help for '{topic}' would be displayed here.");
            Console.WriteLine("This will integrate with the comprehensive help system.");
            Console.WriteLine();
        }
        
        private async Task ShowCommandSuggestions()
        {
            var context = await _stateManager.GetCurrentContextAsync();
            var suggestions = await _suggestionEngine.GetContextualSuggestionsAsync(context);
            
            Console.WriteLine("\n💡 Intelligent Command Suggestions");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            foreach (var suggestion in suggestions.Take(10))
            {
                Console.WriteLine($"🎯 {suggestion.Command}");
                Console.WriteLine($"   {suggestion.Description}");
                Console.WriteLine($"   Category: {suggestion.Category} | Relevance: {suggestion.Relevance:P0}");
                if (!string.IsNullOrEmpty(suggestion.Reason))
                {
                    Console.WriteLine($"   Reason: {suggestion.Reason}");
                }
                Console.WriteLine();
            }
        }
        
        private async Task ShowCommandHistory()
        {
            var history = await _stateManager.GetCommandHistoryAsync();
            
            Console.WriteLine("\n📋 Command History");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            if (!history.Any())
            {
                Console.WriteLine("No commands in history yet.");
                return;
            }
            
            foreach (var cmd in history.Take(20))
            {
                Console.WriteLine($"  {cmd}");
            }
            
            Console.WriteLine();
        }
        
        private async Task ExecuteCommandWithProgress(string[] args)
        {
            // This would integrate with the progress tracking system
            Console.WriteLine($"Executing: {string.Join(" ", args)}");
            Console.WriteLine("(This will integrate with the progress tracking system)");
            
            // For now, just simulate execution
            await Task.Delay(1000);
            Console.WriteLine("✅ Command completed");
        }
    }
}
