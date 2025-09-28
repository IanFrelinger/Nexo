using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Application.Commands.Planning;

namespace Nexo.Core.Application.Commands.Validation
{
    /// <summary>
    /// Engine responsible for validating commands before execution
    /// </summary>
    /// <typeparam name="TInput">The input type for commands</typeparam>
    /// <typeparam name="TOutput">The output type for commands</typeparam>
    public class CommandValidationEngine<TInput, TOutput>
    {
        /// <summary>
        /// Validates all commands and their inputs
        /// </summary>
        public CommandValidationResult Validate(IEnumerable<ICommand<TInput, TOutput>> commands, TInput input)
        {
            var commandList = commands.ToList();
            var errors = new List<string>();
            var warnings = new List<string>();

            // Validate commands collection
            if (commandList.Count == 0)
            {
                errors.Add("No commands provided for execution");
                return new CommandValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings,
                    ExecutionPlan = new CommandExecutionPlan { Commands = commandList.Cast<object>().ToList(), ExecutionOrder = new List<string>() }
                };
            }

            // Validate each command
            foreach (var command in commandList)
            {
                try
                {
                    // Check if command is null
                    if (command == null)
                    {
                        errors.Add("Null command found in the collection");
                        continue;
                    }

                    // Check if command has required properties
                    if (string.IsNullOrWhiteSpace(command.CommandId))
                    {
                        errors.Add($"Command {command.GetType().Name} has empty or null CommandId");
                    }

                    if (string.IsNullOrWhiteSpace(command.CommandName))
                    {
                        errors.Add($"Command {command.GetType().Name} has empty or null CommandName");
                    }

                    if (string.IsNullOrWhiteSpace(command.Description))
                    {
                        warnings.Add($"Command {command.GetType().Name} has empty or null Description");
                    }

                    // Check for duplicate command IDs
                    var duplicateCount = commandList.Count(c => c.CommandId == command.CommandId);
                    if (duplicateCount > 1)
                    {
                        errors.Add($"Duplicate command ID found: {command.CommandId}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error validating command {command.GetType().Name}: {ex.Message}");
                }
            }

            // Validate input
            if (input == null)
            {
                errors.Add("Input cannot be null");
            }

            return new CommandValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                ExecutionPlan = new CommandExecutionPlan { Commands = commandList.Cast<object>().ToList(), ExecutionOrder = commandList.Select(c => c.CommandId).ToList() }
            };
        }

        /// <summary>
        /// Validates a single command
        /// </summary>
        public CommandValidationResult ValidateSingle(ICommand<TInput, TOutput> command, TInput input)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (command == null)
            {
                errors.Add("Command cannot be null");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(command.CommandId))
                {
                    errors.Add("Command ID cannot be null or empty");
                }

                if (string.IsNullOrWhiteSpace(command.CommandName))
                {
                    errors.Add("Command name cannot be null or empty");
                }
            }

            if (input == null)
            {
                errors.Add("Input cannot be null");
            }

            return new CommandValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                ExecutionPlan = new CommandExecutionPlan { Commands = command != null ? new List<object> { command } : new List<object>(), ExecutionOrder = command != null ? new List<string> { command.CommandId } : new List<string>() }
            };
        }
    }
}
