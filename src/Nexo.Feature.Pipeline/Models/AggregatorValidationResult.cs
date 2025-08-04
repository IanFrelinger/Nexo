using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Models;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of aggregator validation.
    /// </summary>
    public class AggregatorValidationResult
{
    /// <summary>
    /// Whether the aggregator validation was successful.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Validation errors found during validation.
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    
    /// <summary>
    /// Validation warnings found during validation.
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
    
    /// <summary>
    /// Additional validation information.
    /// </summary>
    public List<string> Information { get; set; } = new List<string>();
    
    /// <summary>
    /// Validation results for individual behaviors within the aggregator.
    /// </summary>
    public List<BehaviorValidationResult> BehaviorValidationResults { get; set; } = new List<BehaviorValidationResult>();
    
    /// <summary>
    /// Validation results for direct commands within the aggregator.
    /// </summary>
    public List<CommandValidationResult> DirectCommandValidationResults { get; set; } = new List<CommandValidationResult>();
    
    /// <summary>
    /// Creates a valid validation result.
    /// </summary>
    /// <returns>A valid validation result.</returns>
    public static AggregatorValidationResult Valid()
    {
        return new AggregatorValidationResult { IsValid = true };
    }
    
    /// <summary>
    /// Creates an invalid validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>An invalid validation result.</returns>
    public static AggregatorValidationResult Invalid(params ValidationError[] errors)
    {
        return new AggregatorValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
    
    /// <summary>
    /// Creates an invalid validation result with error messages.
    /// </summary>
    /// <param name="errorMessages">The error messages.</param>
    /// <returns>An invalid validation result.</returns>
    public static AggregatorValidationResult Invalid(params string[] errorMessages)
    {
        var errors = errorMessages.Select(msg => new ValidationError(msg)).ToArray();
        return Invalid(errors);
    }
    
    /// <summary>
    /// Adds a validation error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddError(ValidationError error)
    {
        Errors.Add(error);
        IsValid = false;
        return this;
    }
    
    /// <summary>
    /// Adds a validation error with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddError(string message)
    {
        return AddError(new ValidationError(message));
    }
    
    /// <summary>
    /// Adds a validation warning.
    /// </summary>
    /// <param name="warning">The validation warning.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddWarning(ValidationWarning warning)
    {
        Warnings.Add(warning);
        return this;
    }
    
    /// <summary>
    /// Adds a validation warning with a message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddWarning(string message)
    {
        return AddWarning(new ValidationWarning(message));
    }
    
    /// <summary>
    /// Adds validation information.
    /// </summary>
    /// <param name="information">The information message.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddInformation(string information)
    {
        Information.Add(information);
        return this;
    }
    
    /// <summary>
    /// Adds a behavior validation result.
    /// </summary>
    /// <param name="behaviorValidationResult">The behavior validation result to add.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddBehaviorValidationResult(BehaviorValidationResult behaviorValidationResult)
    {
        BehaviorValidationResults.Add(behaviorValidationResult);
        
        // If any behavior validation fails, the aggregator validation fails
        if (!behaviorValidationResult.IsValid)
        {
            IsValid = false;
        }
        
        return this;
    }
    
    /// <summary>
    /// Adds a direct command validation result.
    /// </summary>
    /// <param name="commandValidationResult">The command validation result to add.</param>
    /// <returns>This validation result for chaining.</returns>
    public AggregatorValidationResult AddDirectCommandValidationResult(CommandValidationResult commandValidationResult)
    {
        DirectCommandValidationResults.Add(commandValidationResult);
        
        // If any command validation fails, the aggregator validation fails
        if (!commandValidationResult.IsValid)
        {
            IsValid = false;
        }
        
        return this;
    }
    
    /// <summary>
    /// Gets all validation errors from the aggregator, its behaviors, and its commands.
    /// </summary>
    /// <returns>All validation errors.</returns>
    public IEnumerable<ValidationError> GetAllErrors()
    {
        var allErrors = new List<ValidationError>(Errors);
        
        foreach (var behaviorResult in BehaviorValidationResults)
        {
            allErrors.AddRange(behaviorResult.GetAllErrors());
        }
        
        foreach (var commandResult in DirectCommandValidationResults)
        {
            allErrors.AddRange(commandResult.Errors);
        }
        
        return allErrors;
    }
    
    /// <summary>
    /// Gets all validation warnings from the aggregator, its behaviors, and its commands.
    /// </summary>
    /// <returns>All validation warnings.</returns>
    public IEnumerable<ValidationWarning> GetAllWarnings()
    {
        var allWarnings = new List<ValidationWarning>(Warnings);
        
        foreach (var behaviorResult in BehaviorValidationResults)
        {
            allWarnings.AddRange(behaviorResult.GetAllWarnings());
        }
        
        foreach (var commandResult in DirectCommandValidationResults)
        {
            allWarnings.AddRange(commandResult.Warnings);
        }
        
        return allWarnings;
    }
}
}