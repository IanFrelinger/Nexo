using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Nexo.Shared.Validation
{
    /// <summary>
    /// Extension methods for validation
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Validates an object and returns a result
        /// </summary>
        public static ValidationResult Validate<T>(this T obj, ValidationContext? context = null)
        {
            return DataAnnotationsValidator.Validate(obj, context);
        }

        /// <summary>
        /// Validates required fields
        /// </summary>
        public static ValidationResult ValidateRequired<T>(this T obj, params string[] requiredFields)
        {
            return CustomValidators.ValidateRequired(obj, requiredFields);
        }

        /// <summary>
        /// Validates string length constraints
        /// </summary>
        public static ValidationResult ValidateStringLength<T>(this T obj, Dictionary<string, (int Min, int Max)> lengthConstraints)
        {
            return CustomValidators.ValidateStringLength(obj, lengthConstraints);
        }

        /// <summary>
        /// Combines multiple validation results
        /// </summary>
        public static ValidationResult Combine(this ValidationResult result1, ValidationResult result2)
        {
            return new ValidationResult
            {
                IsValid = result1.IsValid && result2.IsValid,
                Errors = result1.Errors.Concat(result2.Errors).ToList(),
                Warnings = result1.Warnings.Concat(result2.Warnings).ToList()
            };
        }

        /// <summary>
        /// Combines multiple validation results
        /// </summary>
        public static ValidationResult Combine(this IEnumerable<ValidationResult> results)
        {
            var resultList = results.ToList();
            return new ValidationResult
            {
                IsValid = resultList.All(r => r.IsValid),
                Errors = resultList.SelectMany(r => r.Errors).ToList(),
                Warnings = resultList.SelectMany(r => r.Warnings).ToList()
            };
        }
    }
}
