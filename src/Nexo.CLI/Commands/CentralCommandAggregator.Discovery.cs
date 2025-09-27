using System;
using System.CommandLine;
using System.Linq;

namespace Nexo.CLI.Commands
{
    public partial class CentralCommandAggregator
    {
        private Command CreateDiscoveryCommand()
        {
            var discoveryCommand = new Command("discover", "Discover and explore available commands");

            var listCommand = new Command("list", "List all available commands");
            var categoryOption = new Option<string>("--category", "Filter by category");
            listCommand.AddOption(categoryOption);

            listCommand.SetHandler((string category) =>
            {
                ListCommands(category);
            }, categoryOption);

            discoveryCommand.AddCommand(listCommand);

            var searchCommand = new Command("search", "Search for commands");
            var queryArgument = new Argument<string>("query", "Search query");
            searchCommand.AddArgument(queryArgument);

            searchCommand.SetHandler((string query) =>
            {
                SearchCommands(query);
            }, queryArgument);

            discoveryCommand.AddCommand(searchCommand);

            var categoriesCommand = new Command("categories", "List all command categories");
            categoriesCommand.SetHandler(() =>
            {
                ListCategories();
            });

            discoveryCommand.AddCommand(categoriesCommand);

            return discoveryCommand;
        }

        private void ListCommands(string category = null)
        {
            Console.WriteLine("📋 Available Commands");
            Console.WriteLine("====================");
            Console.WriteLine();

            var categoriesToShow = string.IsNullOrEmpty(category) 
                ? _commandCategories.Values 
                : _commandCategories.Values.Where(c => c.Name.Equals(category, StringComparison.OrdinalIgnoreCase));

            foreach (var cat in categoriesToShow)
            {
                Console.WriteLine($"🔹 {cat.Name.ToUpper()} - {cat.Description}");
                foreach (var cmd in cat.Commands)
                {
                    Console.WriteLine($"  • {cmd.Name}: {cmd.Description}");
                    Console.WriteLine($"    Usage: {cmd.Usage}");
                }
                Console.WriteLine();
            }
        }

        private void SearchCommands(string query)
        {
            Console.WriteLine($"🔍 Searching for commands matching: '{query}'");
            Console.WriteLine();

            var results = _commandCategories.Values
                .SelectMany(cat => cat.Commands.Select(cmd => new { Category = cat, Command = cmd }))
                .Where(item => 
                    item.Command.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Command.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    item.Category.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (results.Any())
            {
                foreach (var result in results)
                {
                    Console.WriteLine($"🔹 {result.Category.Name.ToUpper()}");
                    Console.WriteLine($"  • {result.Command.Name}: {result.Command.Description}");
                    Console.WriteLine($"    Usage: {result.Command.Usage}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("❌ No commands found matching your query.");
            }
        }

        private void ListCategories()
        {
            Console.WriteLine("📁 Command Categories");
            Console.WriteLine("====================");
            Console.WriteLine();

            foreach (var category in _commandCategories.Values)
            {
                Console.WriteLine($"🔹 {category.Name.ToUpper()}");
                Console.WriteLine($"   {category.Description}");
                Console.WriteLine($"   Commands: {category.Commands.Count}");
                Console.WriteLine();
            }
        }
    }
}

