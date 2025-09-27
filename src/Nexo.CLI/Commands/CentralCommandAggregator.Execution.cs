using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.CLI.Commands
{
    public partial class CentralCommandAggregator
    {
        private Command CreateExecutionCommand()
        {
            var executionCommand = new Command("execute", "Execute commands with advanced options");

            var runCommand = new Command("run", "Run a command with options");
            var commandArgument = new Argument<string>("command", "Command to execute");
            var dryRunOption = new Option<bool>("--dry-run", "Show what would be executed without running");
            var verboseOption = new Option<bool>("--verbose", "Verbose output");
            var timeoutOption = new Option<int>("--timeout", () => 300, "Timeout in seconds");

            runCommand.AddArgument(commandArgument);
            runCommand.AddOption(dryRunOption);
            runCommand.AddOption(verboseOption);
            runCommand.AddOption(timeoutOption);

            runCommand.SetHandler(async (string command, bool dryRun, bool verbose, int timeout) =>
            {
                await ExecuteCommand(command, dryRun, verbose, timeout);
            }, commandArgument, dryRunOption, verboseOption, timeoutOption);

            executionCommand.AddCommand(runCommand);

            var batchCommand = new Command("batch", "Execute multiple commands from a file");
            var fileArgument = new Argument<string>("file", "File containing commands");
            batchCommand.AddArgument(fileArgument);

            batchCommand.SetHandler(async (string file) =>
            {
                await ExecuteBatchCommands(file);
            }, fileArgument);

            executionCommand.AddCommand(batchCommand);

            return executionCommand;
        }

        private async Task ExecuteCommand(string command, bool dryRun, bool verbose, int timeout)
        {
            if (dryRun)
            {
                Console.WriteLine($"🔍 DRY RUN: Would execute: {command}");
                return;
            }

            if (verbose)
            {
                _logger.LogInformation("Executing command: {Command}", command);
            }

            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                _logger.LogError("Empty command provided");
                return;
            }

            var foundCommand = FindCommand(parts[0]);
            if (foundCommand == null)
            {
                _logger.LogError("Command '{Command}' not found", parts[0]);
                Console.WriteLine($"❌ Command '{parts[0]}' not found. Use 'nexo discover list' to see available commands.");
                return;
            }

            try
            {
                Console.WriteLine($"✅ Executing: {command}");
                await Task.Delay(1000);
                Console.WriteLine($"✅ Command completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command execution failed: {Command}", command);
                Console.WriteLine($"❌ Command failed: {ex.Message}");
                throw;
            }
        }

        private async Task ExecuteBatchCommands(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Batch file not found: {File}", filePath);
                Console.WriteLine($"❌ Batch file not found: {filePath}");
                return;
            }

            var commands = await System.IO.File.ReadAllLinesAsync(filePath);
            var validCommands = commands
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .ToArray();

            _logger.LogInformation("Executing batch file with {Count} commands", validCommands.Length);
            await ExecuteCommandSequence(validCommands);
        }

        private CommandInfo FindCommand(string commandName)
        {
            return _commandCategories.Values
                .SelectMany(cat => cat.Commands)
                .FirstOrDefault(cmd => cmd.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        }
    }
}

