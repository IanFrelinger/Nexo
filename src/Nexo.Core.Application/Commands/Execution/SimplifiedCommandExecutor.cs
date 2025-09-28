using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Commands.Execution
{
    /// <summary>
    /// Simplified command executor following test structure principles:
    /// - No complex retry logic
    /// - No timeout management
    /// - Simple, direct execution
    /// - Clear error handling
    /// </summary>
    public class SimplifiedCommandExecutor
    {
        private readonly Dictionary<string, ISimplifiedCommand<object, object>> _commands = new();

        /// <summary>
        /// Register a command for execution
        /// </summary>
        public void RegisterCommand<TInput, TOutput>(ISimplifiedCommand<TInput, TOutput> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _commands[command.Name] = (ISimplifiedCommand<object, object>)command;
        }

        /// <summary>
        /// Execute a single command (like a single test)
        /// </summary>
        public async Task<CommandResult<TOutput>> ExecuteAsync<TInput, TOutput>(
            string commandName, 
            TInput input, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return CommandResult<TOutput>.Failure("Command name cannot be null or empty", TimeSpan.Zero);

            if (!_commands.TryGetValue(commandName, out var command))
                return CommandResult<TOutput>.Failure($"Command '{commandName}' not found", TimeSpan.Zero);

            var startTime = DateTime.UtcNow;

            try
            {
                // Simple, direct execution - no retries, no timeouts
                var result = await command.ExecuteAsync(input, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                return CommandResult<TOutput>.Failure($"Command execution failed: {ex.Message}", DateTime.UtcNow - startTime);
            }
        }

        /// <summary>
        /// Execute multiple commands in sequence (like running multiple tests)
        /// </summary>
        public async Task<List<CommandResult<object>>> ExecuteSequenceAsync(
            List<(string CommandName, object Input)> commands,
            CancellationToken cancellationToken = default)
        {
            var results = new List<CommandResult<object>>();

            foreach (var (commandName, input) in commands)
            {
                var result = await ExecuteAsync<object, object>(commandName, input, cancellationToken);
                results.Add(result);

                // Stop on first failure (like test failures)
                if (!result.IsSuccess)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Get all registered command names
        /// </summary>
        public IEnumerable<string> GetRegisteredCommands()
        {
            return _commands.Keys;
        }
    }
}
