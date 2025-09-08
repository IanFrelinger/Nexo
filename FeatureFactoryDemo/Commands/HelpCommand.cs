using Microsoft.Extensions.Logging;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Command to display help information for all available commands
    /// </summary>
    public class HelpCommand : BaseCommand
    {
        private readonly Dictionary<string, ICommand> _commands;
        
        public override string Name => "help";
        public override string Description => "Display help information for all available commands";
        public override string Usage => "help [<command-name>]";
        
        public HelpCommand(IServiceProvider serviceProvider, ILogger<HelpCommand> logger, Dictionary<string, ICommand> commands) 
            : base(serviceProvider, logger)
        {
            _commands = commands;
        }
        
        public override async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                Console.WriteLine("📚 Feature Factory Command Help");
                Console.WriteLine("===============================");
                
                if (args.Length > 0)
                {
                    // Show help for specific command
                    var commandName = args[0].ToLower();
                    if (_commands.ContainsKey(commandName))
                    {
                        var command = _commands[commandName];
                        Console.WriteLine($"\n📋 {command.Name} Command");
                        Console.WriteLine("=" + new string('=', command.Name.Length + 8));
                        Console.WriteLine($"Description: {command.Description}");
                        Console.WriteLine($"Usage: {command.Usage}");
                        Console.WriteLine();
                        return 0;
                    }
                    else
                    {
                        DisplayError($"Unknown command: {commandName}");
                        Console.WriteLine("Available commands:");
                        foreach (var cmd in _commands.Keys)
                        {
                            Console.WriteLine($"  - {cmd}");
                        }
                        return 1;
                    }
                }
                
                // Show help for all commands
                Console.WriteLine("Available Commands:");
                Console.WriteLine("==================");
                
                foreach (var command in _commands.Values)
                {
                    Console.WriteLine($"\n📋 {command.Name}");
                    Console.WriteLine($"   Description: {command.Description}");
                    Console.WriteLine($"   Usage: {command.Usage}");
                }
                
                Console.WriteLine("\n📖 General Usage:");
                Console.WriteLine("=================");
                Console.WriteLine("  dotnet run <command> [options]");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  dotnet run analyze --path ../src --limit 50");
                Console.WriteLine("  dotnet run generate --description \"Create a User entity\" --platform DotNet");
                Console.WriteLine("  dotnet run validate --quick");
                Console.WriteLine("  dotnet run stats --all");
                Console.WriteLine("  dotnet run help generate");
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Help command failed");
                DisplayError($"Help command failed: {ex.Message}");
                return 1;
            }
        }
    }
}
