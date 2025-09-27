using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Documentation search functionality
    /// </summary>
    public partial class InteractiveHelpSystem
    {
        public async Task SearchDocumentation(string searchTerm)
        {
            Console.Clear();
            Console.WriteLine($"Search Search Results for '{searchTerm}'");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            try
            {
                var results = await _documentationGenerator.SearchDocumentationAsync(searchTerm);
                
                if (!results.Any())
                {
                    Console.WriteLine("No results found. Try different search terms.");
                    Console.WriteLine();
                    Console.WriteLine("Idea Search Tips:");
                    Console.WriteLine("  • Use specific keywords");
                    Console.WriteLine("  • Try different variations");
                    Console.WriteLine("  • Check spelling");
                    Console.WriteLine();
                }
                else
                {
                    foreach (var result in results.Take(10))
                    {
                        Console.WriteLine($"File {result.Title}");
                        Console.WriteLine($"   {result.Summary}");
                        Console.WriteLine($"   Category: {result.Category} | Relevance: {result.Relevance:P0}");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search documentation for: {SearchTerm}", searchTerm);
                Console.WriteLine($"ERROR: Search failed: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
