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
    /// Input handling functionality
    /// </summary>
    public partial class InteractiveCLI
    {
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
                promptBuilder.Append(" Stats");
            }
            
            if (context.HasPendingAdaptations)
            {
                promptBuilder.Append(" Processing");
            }
            
            if (context.HasPerformanceIssues)
            {
                promptBuilder.Append(" WARNING:");
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
    }
}
