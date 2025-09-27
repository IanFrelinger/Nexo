using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Examples display functionality
    /// </summary>
    public partial class InteractiveHelpSystem
    {
        public async Task ShowExamples(string? category = null)
        {
            Console.Clear();
            Console.WriteLine("Target Examples");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var examples = category != null 
                    ? (await _exampleRepository.GetExamplesByCategory()).GetValueOrDefault(category, new List<CommandExample>())
                    : await _exampleRepository.GetAllExamplesAsync();
                
                if (!examples.Any())
                {
                    Console.WriteLine($"No examples found{(category != null ? $" for category '{category}'" : "")}.");
                    return;
                }
                
                var examplesByCategory = examples.GroupBy(e => e.Category);
                
                foreach (var categoryGroup in examplesByCategory)
                {
                    Console.WriteLine($"Directory {categoryGroup.Key}:");
                    Console.WriteLine();
                    
                    foreach (var example in categoryGroup)
                    {
                        Console.WriteLine($"  Target {example.Title}");
                        Console.WriteLine($"     {example.Description}");
                        Console.WriteLine($"     Command: {example.CommandLine}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load examples for category: {Category}", category);
                Console.WriteLine($"ERROR: Failed to load examples: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
