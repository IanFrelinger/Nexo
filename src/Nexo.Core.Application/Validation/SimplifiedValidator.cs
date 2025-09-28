using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Application.Validation
{
    /// <summary>
    /// Simplified validator following test structure principles:
    /// - Simple validation rules
    /// - Clear pass/fail results
    /// - No complex validation chains
    /// - Direct error reporting
    /// </summary>
    public class SimplifiedValidator<T>
    {
        private readonly List<ValidationRule<T>> _rules = new();

        /// <summary>
        /// Add a validation rule (like adding a test assertion)
        /// </summary>
        public void AddRule(string name, Func<T, bool> predicate, string errorMessage)
        {
            _rules.Add(new ValidationRule<T>
            {
                Name = name,
                Predicate = predicate,
                ErrorMessage = errorMessage
            });
        }

        /// <summary>
        /// Validate an object (like running test assertions)
        /// </summary>
        public ValidationResult Validate(T input)
        {
            var errors = new List<ValidationError>();

            foreach (var rule in _rules)
            {
                try
                {
                    if (!rule.Predicate(input))
                    {
                        errors.Add(new ValidationError
                        {
                            RuleName = rule.Name,
                            Message = rule.ErrorMessage,
                            Severity = ValidationSeverity.Error
                        });
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ValidationError
                    {
                        RuleName = rule.Name,
                        Message = $"Validation rule '{rule.Name}' failed with exception: {ex.Message}",
                        Severity = ValidationSeverity.Error
                    });
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// Get the number of validation rules
        /// </summary>
        public int RuleCount => _rules.Count;

        /// <summary>
        /// Clear all validation rules
        /// </summary>
        public void Clear()
        {
            _rules.Clear();
        }
    }

    /// <summary>
    /// A validation rule (like a test assertion)
    /// </summary>
    public class ValidationRule<T>
    {
        public string Name { get; set; } = string.Empty;
        public Func<T, bool> Predicate { get; set; } = _ => true;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of validation (like test result)
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
    }

    /// <summary>
    /// A validation error (like test failure)
    /// </summary>
    public class ValidationError
    {
        public string RuleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
    }
}
