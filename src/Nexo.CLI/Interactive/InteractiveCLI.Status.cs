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
    /// Status and help functionality
    /// </summary>
    public partial class InteractiveCLI
    {
        public async Task ShowContextualHelpAsync()
        {
            var context = await _stateManager.GetCurrentContextAsync();
            var suggestions = await _suggestionEngine.GetContextualSuggestionsAsync(context);
            
            Console.WriteLine("\nSearch Contextual Help");
            Console.WriteLine("═══════════════════");
            Console.WriteLine();
            
            if (context.CurrentProject != null)
            {
                Console.WriteLine($"Directory Current Project: {context.CurrentProject.Name} ({context.CurrentProject.Type})");
            }
            
            if (!string.IsNullOrEmpty(context.CurrentPlatform))
            {
                Console.WriteLine($"System:  Current Platform: {context.CurrentPlatform}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Idea Suggested Commands:");
            
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
            
            Console.WriteLine("\nStats System Status");
            Console.WriteLine("═══════════════════");
            Console.WriteLine();
            
            Console.WriteLine($"Directory Working Directory: {context.WorkingDirectory}");
            Console.WriteLine($"🕒 Last Activity: {context.LastActivity:yyyy-MM-dd HH:mm:ss}");
            
            if (context.CurrentProject != null)
            {
                Console.WriteLine($"Directory Current Project: {context.CurrentProject.Name}");
                Console.WriteLine($"   Type: {context.CurrentProject.Type}");
                Console.WriteLine($"   Path: {context.CurrentProject.Path}");
                Console.WriteLine($"   Last Modified: {context.CurrentProject.LastModified:yyyy-MM-dd HH:mm:ss}");
            }
            
            if (!string.IsNullOrEmpty(context.CurrentPlatform))
            {
                Console.WriteLine($"System:  Current Platform: {context.CurrentPlatform}");
            }
            
            Console.WriteLine($"Progress Active Monitoring: {(context.HasActiveMonitoring ? "Yes" : "No")}");
            Console.WriteLine($"Processing Pending Adaptations: {(context.HasPendingAdaptations ? "Yes" : "No")}");
            Console.WriteLine($"WARNING:  Performance Issues: {(context.HasPerformanceIssues ? "Yes" : "No")}");
            
            Console.WriteLine();
            Console.WriteLine("List Recent Commands:");
            foreach (var cmd in context.RecentCommands.Take(5))
            {
                Console.WriteLine($"  • {cmd}");
            }
            
            Console.WriteLine();
        }
    }
}
