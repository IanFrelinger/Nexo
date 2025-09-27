using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Command help functionality
    /// </summary>
    public partial class InteractiveHelpSystem
    {
        public async Task ShowCommandHelp(string commandName)
        {
            Console.Clear();
            Console.WriteLine($"📖 Help for: {commandName}");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var documentation = await _documentationGenerator.GenerateCommandDocumentationAsync(commandName);
                Console.WriteLine(documentation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate documentation for command: {Command}", commandName);
                Console.WriteLine($"ERROR: Failed to load documentation for '{commandName}': {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
        
        public async Task ShowCommandBrowser()
        {
            Console.Clear();
            Console.WriteLine("📖 Command Browser");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var topics = await _documentationGenerator.GetAvailableTopicsAsync();
                var categories = topics.GroupBy(t => t.Category).OrderBy(g => g.Key);
                
                foreach (var category in categories)
                {
                    Console.WriteLine($"Tool {category.Key}:");
                    Console.WriteLine();
                    
                    foreach (var topic in category.OrderBy(t => t.Name))
                    {
                        Console.WriteLine($"  • {topic.Name.PadRight(20)} - {topic.Description}");
                    }
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load command browser");
                Console.WriteLine($"ERROR: Failed to load command browser: {ex.Message}");
            }
            
            Console.WriteLine("Idea Type 'nexo <command> --help' for detailed information about any command");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
