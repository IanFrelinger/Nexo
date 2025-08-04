using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Models;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of command validation.
    /// </summary>
    public class CommandValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public List<string> Information { get; set; } = new List<string>();
        public static CommandValidationResult Valid()
        {
            return new CommandValidationResult { IsValid = true };
        }
        public static CommandValidationResult Invalid(params ValidationError[] errors)
        {
            return new CommandValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }
        public static CommandValidationResult Invalid(params string[] errorMessages)
        {
            var errors = errorMessages.Select(msg => new ValidationError(msg)).ToArray();
            return Invalid(errors);
        }
        public CommandValidationResult AddError(ValidationError error)
        {
            Errors.Add(error);
            IsValid = false;
            return this;
        }
        public CommandValidationResult AddError(string message)
        {
            return AddError(new ValidationError(message));
        }
        public CommandValidationResult AddWarning(ValidationWarning warning)
        {
            Warnings.Add(warning);
            return this;
        }
        public CommandValidationResult AddWarning(string message)
        {
            return AddWarning(new ValidationWarning(message));
        }
        public CommandValidationResult AddInformation(string information)
        {
            Information.Add(information);
            return this;
        }
    }
} 