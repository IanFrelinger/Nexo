using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Models;
namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents the result of behavior validation.
    /// </summary>
    public class BehaviorValidationResult
    {
        /// <summary>
        /// Whether the behavior validation was successful.
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
        /// Validation results for individual commands within the behavior.
        /// </summary>
        public List<CommandValidationResult> CommandValidationResults { get; set; } = new List<CommandValidationResult>();
        
        /// <summary>
        /// Creates a valid validation result.
        /// </summary>
        /// <returns>A valid validation result.</returns>
        public static BehaviorValidationResult Valid()
        {
            return new BehaviorValidationResult { IsValid = true };
        }
        
        /// <summary>
        /// Creates an invalid validation result with errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        /// <returns>An invalid validation result.</returns>
        public static BehaviorValidationResult Invalid(params ValidationError[] errors)
        {
            return new BehaviorValidationResult
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
        public static BehaviorValidationResult Invalid(params string[] errorMessages)
        {
            var errors = errorMessages.Select(msg => new ValidationError(msg)).ToArray();
            return Invalid(errors);
        }
        
        /// <summary>
        /// Adds a validation error.
        /// </summary>
        /// <param name="error">The validation error.</param>
        /// <returns>This validation result for chaining.</returns>
        public BehaviorValidationResult AddError(ValidationError error)
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
        public BehaviorValidationResult AddError(string message)
        {
            return AddError(new ValidationError(message));
        }
        
        /// <summary>
        /// Adds a validation warning.
        /// </summary>
        /// <param name="warning">The validation warning.</param>
        /// <returns>This validation result for chaining.</returns>
        public BehaviorValidationResult AddWarning(ValidationWarning warning)
        {
            Warnings.Add(warning);
            return this;
        }
        
        /// <summary>
        /// Adds a validation warning with a message.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <returns>This validation result for chaining.</returns>
        public BehaviorValidationResult AddWarning(string message)
        {
            return AddWarning(new ValidationWarning(message));
        }
        
        /// <summary>
        /// Adds validation information.
        /// </summary>
        /// <param name="information">The information message.</param>
        /// <returns>This validation result for chaining.</returns>
        public BehaviorValidationResult AddInformation(string information)
        {
            Information.Add(information);
            return this;
        }
        
        /// <summary>
        /// Adds a command validation result.
        /// </summary>
        /// <param name="commandValidationResult">The command validation result to add.</param>
        /// <returns>This validation result for chaining.</returns>
        public BehaviorValidationResult AddCommandValidationResult(CommandValidationResult commandValidationResult)
        {
            CommandValidationResults.Add(commandValidationResult);
            
            // If any command validation fails, the behavior validation fails
            if (!commandValidationResult.IsValid)
            {
                IsValid = false;
            }
            
            return this;
        }
        
        /// <summary>
        /// Adds multiple command validation results.
        /// </summary>
        /// <param name="commandValidationResults">The command validation results to add.</param>
        /// <returns>This validation result for chaining.</returns>
        public BehaviorValidationResult AddCommandValidationResults(IEnumerable<CommandValidationResult> commandValidationResults)
        {
            foreach (var result in commandValidationResults)
            {
                AddCommandValidationResult(result);
            }
            return this;
        }
        
        /// <summary>
        /// Gets all validation errors from both the behavior and its commands.
        /// </summary>
        /// <returns>All validation errors.</returns>
        public IEnumerable<ValidationError> GetAllErrors()
        {
            var allErrors = new List<ValidationError>(Errors);
            
            foreach (var commandResult in CommandValidationResults)
            {
                allErrors.AddRange(commandResult.Errors);
            }
            
            return allErrors;
        }
        
        /// <summary>
        /// Gets all validation warnings from both the behavior and its commands.
        /// </summary>
        /// <returns>All validation warnings.</returns>
        public IEnumerable<ValidationWarning> GetAllWarnings()
        {
            var allWarnings = new List<ValidationWarning>(Warnings);
            
            foreach (var commandResult in CommandValidationResults)
            {
                allWarnings.AddRange(commandResult.Warnings);
            }
            
            return allWarnings;
        }
    }
} 