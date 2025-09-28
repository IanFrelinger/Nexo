using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Nexo.Shared.Validation
{
    /// <summary>
    /// Validator for data annotations
    /// </summary>
    public static class DataAnnotationsValidator
    {
        /// <summary>
        /// Validates an object using data annotations
        /// </summary>
        public static ValidationResult Validate<T>(T obj, ValidationContext? context = null)
        {
            var results = new List<ValidationError>();
            
            // Data annotations validation
            var dataAnnotationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = context ?? new ValidationContext(obj);
            
            if (!Validator.TryValidateObject(obj, validationContext, dataAnnotationResults, true))
            {
                results.AddRange(dataAnnotationResults.Select(r => new ValidationError
                {
                    PropertyName = string.Join(",", r.MemberNames),
                    ErrorMessage = r.ErrorMessage ?? "Validation failed",
                    Severity = ValidationSeverity.Error
                }));
            }

            return new ValidationResult
            {
                IsValid = results.Count == 0,
                Errors = results
            };
        }

        /// <summary>
        /// Validates a collection of objects
        /// </summary>
        public static ValidationResult ValidateCollection<T>(IEnumerable<T> collection, ValidationContext? context = null)
        {
            var allResults = new List<ValidationError>();
            var index = 0;

            foreach (var item in collection)
            {
                var itemContext = new ValidationContext(item);
                itemContext.MemberName = $"[{index}]";
                
                var result = Validate(item, itemContext);
                allResults.AddRange(result.Errors);
                index++;
            }

            return new ValidationResult
            {
                IsValid = allResults.Count == 0,
                Errors = allResults
            };
        }
    }
}
