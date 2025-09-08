using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Manages all available commands and handles command execution
    /// </summary>
    public class CommandManager
    {
        private readonly Dictionary<string, ICommand> _commands;
        private readonly ILogger<CommandManager> _logger;
        
        public CommandManager(IServiceProvider serviceProvider, ILogger<CommandManager> logger)
        {
            _logger = logger;
            _commands = new Dictionary<string, ICommand>();
            
            // Register all commands
            RegisterCommands(serviceProvider);
        }
        
        private void RegisterCommands(IServiceProvider serviceProvider)
        {
            // Register commands
            _commands["analyze"] = new AnalyzeCommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<AnalyzeCommand>>());
            _commands["generate"] = new GenerateCommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<GenerateCommand>>());
            _commands["generate-e2e"] = new GenerateWithE2ECommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<GenerateWithE2ECommand>>());
            _commands["validate"] = new ValidateCommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<ValidateCommand>>());
            _commands["stats"] = new StatsCommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<StatsCommand>>());
            _commands["help"] = new HelpCommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<HelpCommand>>(), _commands);
            _commands["test-logging"] = new LoggingTestCommand(serviceProvider, serviceProvider.GetRequiredService<ILogger<LoggingTestCommand>>());
            
            _logger.LogInformation("Registered {CommandCount} commands", _commands.Count);
        }
        
        /// <summary>
        /// Execute a command with the given arguments
        /// </summary>
        /// <param name="commandName">Name of the command to execute</param>
        /// <param name="args">Command arguments</param>
        /// <returns>Exit code (0 for success, non-zero for failure)</returns>
        public async Task<int> ExecuteCommandAsync(string commandName, string[] args)
        {
            try
            {
                if (!_commands.ContainsKey(commandName.ToLower()))
                {
                    Console.WriteLine($"❌ Unknown command: {commandName}");
                    Console.WriteLine("Available commands:");
                    foreach (var cmd in _commands.Keys)
                    {
                        Console.WriteLine($"  - {cmd}");
                    }
                    Console.WriteLine("Use 'help' command for more information.");
                    return 1;
                }
                
                var command = _commands[commandName.ToLower()];
                _logger.LogInformation("Executing command: {CommandName}", commandName);
                
                return await command.ExecuteAsync(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command execution failed for: {CommandName}", commandName);
                Console.WriteLine($"❌ Command execution failed: {ex.Message}");
                return 1;
            }
        }
        
        /// <summary>
        /// Get all available commands
        /// </summary>
        /// <returns>Dictionary of command names and their instances</returns>
        public Dictionary<string, ICommand> GetCommands()
        {
            return _commands;
        }
        
        /// <summary>
        /// Check if a command exists
        /// </summary>
        /// <param name="commandName">Name of the command to check</param>
        /// <returns>True if command exists, false otherwise</returns>
        public bool CommandExists(string commandName)
        {
            return _commands.ContainsKey(commandName.ToLower());
        }
    }
}
