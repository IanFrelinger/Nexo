using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Default implementation of composable command orchestrator
    /// </summary>
    public class ComposableOrchestrator : IComposableOrchestrator
    {
        private readonly Dictionary<ICommandIdentifier, IComposableCommand> _commands;
        private readonly ICollectionOperations _collectionOps;
        private readonly ILoopOperations _loopOps;
        
        public ComposableOrchestrator(
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null)
        {
            _commands = new Dictionary<ICommandIdentifier, IComposableCommand>();
            _collectionOps = collectionOps ?? new CollectionOperations();
            _loopOps = loopOps ?? new LoopOperations();
        }
        
        public async Task<ICommandResult> RegisterCommandAsync(IComposableCommand command)
        {
            if (command == null)
                return CommandResult.Failure(new CommandIdentifier("REGISTRATION_ERROR", "Nexo.Shared.Composable", "1.0.0"), "Command cannot be null");
            
            if (_commands.ContainsKey(command.Id))
                return CommandResult.Failure(command.Id, "Command is already registered");
            
            _commands[command.Id] = command;
            return CommandResult.Success(command.Id, "Command registered successfully");
        }
        
        public async Task<ICommandResult> UnregisterCommandAsync(ICommandIdentifier commandId)
        {
            if (commandId == null)
                return CommandResult.Failure(new CommandIdentifier("UNREGISTRATION_ERROR", "Nexo.Shared.Composable", "1.0.0"), "Command ID cannot be null");
            
            if (!_commands.ContainsKey(commandId))
                return CommandResult.Failure(commandId, "Command is not registered");
            
            _commands.Remove(commandId);
            return CommandResult.Success(commandId, "Command unregistered successfully");
        }
        
        public async Task<ICommandResult> ExecuteCommandAsync(ICommandIdentifier commandId, ICommandContext context)
        {
            if (commandId == null)
                return CommandResult.Failure(new CommandIdentifier("EXECUTION_ERROR", "Nexo.Shared.Composable", "1.0.0"), "Command ID cannot be null");
            
            if (!_commands.TryGetValue(commandId, out var command))
                return CommandResult.Failure(commandId, "Command is not registered");
            
            try
            {
                // Validate command
                var validationResult = await command.ValidateAsync(context);
                if (!validationResult.IsValid)
                {
                    return CommandResult.Failure(commandId, "Command validation failed", validationResult.Errors);
                }
                
                // Execute command
                return await command.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(commandId, $"Command execution failed: {ex.Message}");
            }
        }
        
        public async Task<IReadOnlyList<ICommandResult>> ExecuteSequenceAsync(IReadOnlyList<ICommandIdentifier> commandIds, ICommandContext context)
        {
            var results = new List<ICommandResult>();
            
            await _loopOps.ForEachAsync(commandIds, async (commandId, index) =>
            {
                var result = await ExecuteCommandAsync(commandId, context);
                results.Add(result);
                
                // Stop execution if a command fails
                if (!result.IsSuccess)
                {
                    return; // This will break the loop
                }
            });
            
            return results;
        }
        
        public async Task<IReadOnlyList<ICommandResult>> ExecuteParallelAsync(IReadOnlyList<ICommandIdentifier> commandIds, ICommandContext context)
        {
            var tasks = new List<Task<ICommandResult>>();
            
            await _loopOps.ForEachAsync(commandIds, async (commandId, index) =>
            {
                tasks.Add(ExecuteCommandAsync(commandId, context));
            });
            
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
        
        public async Task<IWorkflowResult> ExecuteWorkflowAsync(IWorkflowDefinition workflow, ICommandContext context)
        {
            var startTime = DateTime.UtcNow;
            var stepResults = new List<ICommandResult>();
            var metadata = new Dictionary<string, object>();
            
            try
            {
                // Execute workflow steps
                await _loopOps.ForEachAsync(workflow.Steps, async (step, index) =>
                {
                    var stepContext = new CommandContext(
                        step.CommandId,
                        step.Parameters,
                        context.Metadata,
                        context.PreviousResults,
                        context.DependentResults,
                        context.CorrelationId,
                        context);
                    
                    var result = await ExecuteCommandAsync(step.CommandId, stepContext);
                    stepResults.Add(result);
                    
                    // Stop if step fails and is not optional
                    if (!result.IsSuccess && !step.IsOptional)
                    {
                        return; // This will break the loop
                    }
                });
                
                var isSuccess = stepResults.All(r => r.IsSuccess || stepResults.Any(sr => sr.CommandId == r.CommandId && workflow.Steps.Any(ws => ws.CommandId == sr.CommandId && ws.IsOptional)));
                
                return new WorkflowResult(
                    isSuccess,
                    isSuccess ? "Workflow completed successfully" : "Workflow completed with errors",
                    stepResults,
                    metadata,
                    DateTime.UtcNow,
                    DateTime.UtcNow - startTime);
            }
            catch (Exception ex)
            {
                return new WorkflowResult(
                    false,
                    $"Workflow execution failed: {ex.Message}",
                    stepResults,
                    metadata,
                    DateTime.UtcNow,
                    DateTime.UtcNow - startTime);
            }
        }
        
        public async Task<IReadOnlyList<IComposableCommand>> GetRegisteredCommandsAsync()
        {
            await Task.CompletedTask;
            return _commands.Values.ToList();
        }
        
        public async Task<IComposableCommand> GetCommandAsync(ICommandIdentifier commandId)
        {
            await Task.CompletedTask;
            return _commands.TryGetValue(commandId, out var command) ? command : null;
        }
        
        public async Task<IValidationResult> ValidateExecutionPlanAsync(IReadOnlyList<ICommandIdentifier> commandIds, ICommandContext context)
        {
            var errors = new List<IValidationError>();
            var warnings = new List<IValidationWarning>();
            
            // Check if all commands are registered
            await _loopOps.ForEachAsync(commandIds, async (commandId, index) =>
            {
                if (!_commands.ContainsKey(commandId))
                {
                    errors.Add(new ValidationError("COMMAND_NOT_REGISTERED", $"Command {commandId} is not registered", "CommandId", "Error"));
                }
            });
            
            // Check dependencies
            await _loopOps.ForEachAsync(commandIds, async (commandId, index) =>
            {
                if (_commands.TryGetValue(commandId, out var command))
                {
                    var dependencies = command.Dependencies;
                    
                    // Check required dependencies
                    await _loopOps.ForEachAsync(dependencies.RequiredCommands, async (depId, depIndex) =>
                    {
                        if (!commandIds.Contains(depId))
                        {
                            errors.Add(new ValidationError("MISSING_DEPENDENCY", $"Required dependency {depId} is missing", "Dependencies", "Error"));
                        }
                    });
                    
                    // Check conflicting commands
                    await _loopOps.ForEachAsync(dependencies.ConflictingCommands, async (conflictId, conflictIndex) =>
                    {
                        if (commandIds.Contains(conflictId))
                        {
                            errors.Add(new ValidationError("CONFLICTING_COMMAND", $"Command {commandId} conflicts with {conflictId}", "Dependencies", "Error"));
                        }
                    });
                }
            });
            
            return new ValidationResult(errors.Count == 0, errors, warnings);
        }
    }
}
