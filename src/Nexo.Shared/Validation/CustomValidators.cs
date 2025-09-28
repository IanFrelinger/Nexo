using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nexo.Shared.Validation
{
    /// <summary>
    /// Custom validation rules
    /// </summary>
    public static class CustomValidators
    {
        /// <summary>
        /// Validates required fields
        /// </summary>
        public static ValidationResult ValidateRequired<T>(T obj, params string[] requiredFields)
        {
            var results = new List<ValidationError>();
            var type = typeof(T);

            foreach (var fieldName in requiredFields)
            {
                var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    var value = property.GetValue(obj);
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                    {
                        results.Add(new ValidationError
                        {
                            PropertyName = fieldName,
                            ErrorMessage = $"{fieldName} is required",
                            Severity = ValidationSeverity.Error
                        });
                    }
                }
            }

            return new ValidationResult
            {
                IsValid = results.Count == 0,
                Errors = results
            };
        }

        /// <summary>
        /// Validates string length constraints
        /// </summary>
        public static ValidationResult ValidateStringLength<T>(T obj, Dictionary<string, (int Min, int Max)> lengthConstraints)
        {
            var results = new List<ValidationError>();
            var type = typeof(T);

            foreach (var (fieldName, (min, max)) in lengthConstraints)
            {
                var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(obj) as string;
                    if (value != null)
                    {
                        if (value.Length < min)
                        {
                            results.Add(new ValidationError
                            {
                                PropertyName = fieldName,
                                ErrorMessage = $"{fieldName} must be at least {min} characters",
                                Severity = ValidationSeverity.Error
                            });
                        }
                        else if (value.Length > max)
                        {
                            results.Add(new ValidationError
                            {
                                PropertyName = fieldName,
                                ErrorMessage = $"{fieldName} must be no more than {max} characters",
                                Severity = ValidationSeverity.Error
                            });
                        }
                    }
                }
            }

            return new ValidationResult
            {
                IsValid = results.Count == 0,
                Errors = results
            };
        }
    }
}
