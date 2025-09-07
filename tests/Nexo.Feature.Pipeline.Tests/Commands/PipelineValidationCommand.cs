using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Pipeline.Tests.Commands;

/// <summary>
/// Command for validating Pipeline functionality with proper logging and timeouts.
/// </summary>
public class PipelineValidationCommand
{
    private readonly ILogger<PipelineValidationCommand> _logger;

    public PipelineValidationCommand(ILogger<PipelineValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates Pipeline interface definitions.
    /// </summary>
    public bool ValidatePipelineInterfaces(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Pipeline interface validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var iCommandType = typeof(ICommand);
            var iBehaviorType = typeof(IBehavior);
            var iAggregatorType = typeof(IAggregator);
            var iCommandMethods = iCommandType.GetMethods();
            var iBehaviorMethods = iBehaviorType.GetMethods();
            var iAggregatorMethods = iAggregatorType.GetMethods();
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Pipeline interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = iCommandType.IsInterface &&
                        iBehaviorType.IsInterface &&
                        iAggregatorType.IsInterface &&
                        iCommandMethods.Length > 0 &&
                        iBehaviorMethods.Length > 0 &&
                        iAggregatorMethods.Length > 0;
            _logger.LogInformation("Pipeline interface validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pipeline interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Pipeline model classes.
    /// </summary>
    public bool ValidatePipelineModels(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Pipeline models validation");
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Test core model instantiation
            var pipelineConfig = new PipelineConfiguration();
            var commandMetadata = new CommandMetadata();
            var behaviorMetadata = new BehaviorMetadata();
            var aggregatorMetadata = new AggregatorMetadata();
            var executionResult = new PipelineExecutionResult();
            var executionPlan = new PipelineExecutionPlan();
            var executionStep = new PipelineExecutionStep();
            var executionPhase = new PipelineExecutionPhase();
            
            // Test enum-based models
            _ = CommandCategory.Analysis;
            _ = BehaviorExecutionStrategy.Sequential;
            _ = AggregatorExecutionStrategy.Parallel;
            _ = ExecutionStatus.Pending;
            _ = CommandPriority.Normal;
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Pipeline models validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            
            var result = pipelineConfig != null &&
                        commandMetadata != null &&
                        behaviorMetadata != null &&
                        aggregatorMetadata != null &&
                        executionResult != null &&
                        executionPlan != null &&
                        executionStep != null &&
                        executionPhase != null;
            
            _logger.LogInformation("Pipeline models validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pipeline models validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Pipeline execution context functionality.
    /// </summary>
    public bool ValidatePipelineExecutionContext(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Pipeline execution context validation");
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Test execution context creation and basic operations
            var configuration = new PipelineConfiguration();
            var context = new PipelineContext(_logger, configuration);
            
            // Test context property assignments
            context.Status = PipelineExecutionStatus.Executing;
            context.Status = PipelineExecutionStatus.Completed;
            
            // Test execution result creation
            var result = new PipelineExecutionResult
            {
                ExecutionId = context.ExecutionId,
                Status = ExecutionStatus.Completed,
                StartTime = context.StartTime,
                EndTime = DateTime.UtcNow,
                IsSuccess = true
            };
            
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Pipeline execution context validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            
            var validationResult = !string.IsNullOrEmpty(context.ExecutionId) &&
                                 context.StartTime != default &&
                                 context.Status == PipelineExecutionStatus.Completed &&
                                 result.ExecutionId == context.ExecutionId &&
                                 result.IsSuccess;
            
            _logger.LogInformation("Pipeline execution context validation completed: {Result}", validationResult);
            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pipeline execution context validation");
            return false;
        }
    }

    /// <summary>
    /// Validates Pipeline enum values.
    /// </summary>
    public bool ValidatePipelineEnums(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting Pipeline enum validation");
        try
        {
            var startTime = DateTime.UtcNow;
            var build = Enum.IsDefined(typeof(CommandCategory), CommandCategory.Build);
            var analysis = Enum.IsDefined(typeof(CommandCategory), CommandCategory.Analysis);
            var sequential = Enum.IsDefined(typeof(BehaviorExecutionStrategy), BehaviorExecutionStrategy.Sequential);
            var parallel = Enum.IsDefined(typeof(BehaviorExecutionStrategy), BehaviorExecutionStrategy.Parallel);
            var conditional = Enum.IsDefined(typeof(BehaviorExecutionStrategy), BehaviorExecutionStrategy.Conditional);
            var custom = Enum.IsDefined(typeof(AggregatorExecutionStrategy), AggregatorExecutionStrategy.Custom);
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Pipeline enum validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }
            var result = build && analysis && sequential && parallel && conditional && custom;
            _logger.LogInformation("Pipeline enum validation completed: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Pipeline enum validation");
            return false;
        }
    }
} 