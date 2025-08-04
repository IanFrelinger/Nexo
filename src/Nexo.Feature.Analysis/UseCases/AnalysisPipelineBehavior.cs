using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Enums;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Feature.Analysis.UseCases
{
    /// <summary>
    /// Pipeline behavior for analysis-specific operations and validations.
    /// Integrates with the pipeline architecture to provide analysis behaviors.
    /// </summary>
    public class AnalysisPipelineBehavior : IBehavior
    {
        private readonly ILogger<AnalysisPipelineBehavior> _logger;
        private readonly List<ICommand> _commands;
        private readonly List<BehaviorDependency> _dependencies;

        public string Id => "analysis-pipeline-behavior";
        public string Name => "Analysis Pipeline Behavior";
        public string Description => "Orchestrates analysis operations including code quality and architecture analysis";
        public BehaviorCategory Category => BehaviorCategory.Analysis;
        public IReadOnlyList<string> Tags => new List<string> { "analysis", "pipeline", "orchestration", "quality" };
        public BehaviorExecutionStrategy ExecutionStrategy => BehaviorExecutionStrategy.Sequential;
        public IReadOnlyList<ICommand> Commands => _commands.AsReadOnly();
        public IReadOnlyList<BehaviorDependency> Dependencies => _dependencies.AsReadOnly();
        public bool CanExecuteInParallel => false;

        public AnalysisPipelineBehavior(ILogger<AnalysisPipelineBehavior> logger)
        {
            _logger = logger;
            _commands = new List<ICommand>();
            _dependencies = new List<BehaviorDependency>();
        }

        public async Task<BehaviorValidationResult> ValidateAsync(IPipelineContext context)
        {
            try
            {
                _logger.LogInformation("Validating AnalysisPipelineBehavior");

                if (context == null)
                {
                    return BehaviorValidationResult.Invalid("Pipeline context is required");
                }

                if (!_commands.Any())
                {
                    return BehaviorValidationResult.Invalid("No commands configured for analysis behavior");
                }

                // Validate each command
                var commandValidationResults = new List<Nexo.Shared.Models.CommandValidationResult>();
                foreach (var command in _commands)
                {
                    var validationResult = await command.ValidateAsync(context);
                    commandValidationResults.Add(validationResult);
                    
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Command {CommandId} validation failed", command.Id);
                    }
                }

                var result = BehaviorValidationResult.Valid();
                result.CommandValidationResults.AddRange(commandValidationResults);

                // Check if any commands failed validation
                if (commandValidationResults.Any(r => !r.IsValid))
                {
                    result.IsValid = false;
                    result.AddError("One or more commands failed validation");
                }

                _logger.LogInformation("AnalysisPipelineBehavior validation completed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating AnalysisPipelineBehavior");
                return BehaviorValidationResult.Invalid($"Validation error: {ex.Message}");
            }
        }

        public async Task<BehaviorResult> ExecuteAsync(IPipelineContext context)
        {
            var startTime = DateTime.UtcNow;
            var commandResults = new List<CommandResult>();

            try
            {
                _logger.LogInformation("Executing AnalysisPipelineBehavior with {CommandCount} commands", _commands.Count);

                foreach (var command in _commands)
                {
                    _logger.LogInformation("Executing command: {CommandId}", command.Id);
                    
                    var commandResult = await command.ExecuteAsync(context);
                    commandResults.Add(commandResult);

                    if (!commandResult.IsSuccess)
                    {
                        _logger.LogWarning("Command {CommandId} failed: {ErrorMessage}", command.Id, commandResult.ErrorMessage);
                        
                        // Continue with other commands unless this is a critical failure
                        if (command.Priority == CommandPriority.Critical)
                        {
                            var failureEndTime = DateTime.UtcNow;
                            var failureExecutionTime = (long)(failureEndTime - startTime).TotalMilliseconds;
                            
                            return BehaviorResult.Failure($"Critical command {command.Id} failed: {commandResult.ErrorMessage}", 
                                commandResult.Exception, failureExecutionTime, startTime, failureEndTime)
                                .AddCommandResults(commandResults);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Command {CommandId} executed successfully", command.Id);
                    }
                }

                var endTime = DateTime.UtcNow;
                var executionTime = (long)(endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("AnalysisPipelineBehavior executed successfully in {ExecutionTime}ms", executionTime);

                return BehaviorResult.Success(commandResults, executionTime, startTime, endTime)
                    .AddCommandResults(commandResults)
                    .AddInformation($"Executed {commandResults.Count} commands successfully");
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                var executionTime = (long)(endTime - startTime).TotalMilliseconds;
                
                _logger.LogError(ex, "Error executing AnalysisPipelineBehavior");
                return BehaviorResult.Failure($"Behavior execution failed: {ex.Message}", ex, executionTime, startTime, endTime)
                    .AddCommandResults(commandResults);
            }
        }

        public async Task CleanupAsync(IPipelineContext context)
        {
            try
            {
                _logger.LogInformation("Cleaning up AnalysisPipelineBehavior");
                
                // Clean up each command
                foreach (var command in _commands)
                {
                    try
                    {
                        await command.CleanupAsync(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error cleaning up command {CommandId}", command.Id);
                    }
                }
                
                _logger.LogInformation("AnalysisPipelineBehavior cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AnalysisPipelineBehavior cleanup");
            }
        }

        public BehaviorMetadata GetMetadata()
        {
            return new BehaviorMetadata
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Category = Category,
                Tags = Tags.ToList(),
                ExecutionStrategy = ExecutionStrategy,
                CanExecuteInParallel = CanExecuteInParallel,
                Dependencies = Dependencies.ToList(),
                CommandCount = _commands.Count,
                Version = "1.0.0",
                Author = "Nexo Analysis Team"
            };
        }

        public void AddCommand(ICommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (_commands.Any(c => c.Id == command.Id))
            {
                _logger.LogWarning("Command with ID {CommandId} already exists in behavior", command.Id);
                return;
            }

            _commands.Add(command);
            _logger.LogInformation("Added command {CommandId} to analysis behavior", command.Id);
        }

        public bool RemoveCommand(string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
            {
                throw new ArgumentException("Command ID cannot be null or empty", nameof(commandId));
            }

            var command = _commands.FirstOrDefault(c => c.Id == commandId);
            if (command != null)
            {
                _commands.Remove(command);
                _logger.LogInformation("Removed command {CommandId} from analysis behavior", commandId);
                return true;
            }

            _logger.LogWarning("Command with ID {CommandId} not found in behavior", commandId);
            return false;
        }

        public async Task<BehaviorExecutionPlan> GetExecutionPlanAsync(IPipelineContext context)
        {
            try
            {
                _logger.LogInformation("Generating execution plan for AnalysisPipelineBehavior");

                var plan = new BehaviorExecutionPlan
                {
                    BehaviorId = Id,
                    ExecutionStrategy = ExecutionStrategy,
                    CanExecuteInParallel = CanExecuteInParallel
                };

                _logger.LogInformation("Generated execution plan with {CommandCount} commands", _commands.Count);
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating execution plan for AnalysisPipelineBehavior");
                throw;
            }
        }
    }
} 