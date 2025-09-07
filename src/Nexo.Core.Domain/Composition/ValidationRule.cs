using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Represents a validation rule that can be applied to objects and composed with other validation rules.
    /// </summary>
    public class ValidationRule : IComposable<ValidationRule>
    {
        private readonly string _name;
        private readonly string _description;
        private readonly ValidationType _type;
        private readonly string _expression;
        private readonly string _errorMessage;
        private readonly ValidationSeverity _severity;
        private readonly Func<object, bool> _validationFunc;
        private readonly List<ValidationRule> _composedRules = new();
        
        /// <summary>
        /// Gets the name of this validation rule.
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// Gets the description of this validation rule.
        /// </summary>
        public string Description => _description;
        
        /// <summary>
        /// Gets the type of this validation rule.
        /// </summary>
        public ValidationType Type => _type;
        
        /// <summary>
        /// Gets the expression used for validation.
        /// </summary>
        public string Expression => _expression;
        
        /// <summary>
        /// Gets the error message to display when validation fails.
        /// </summary>
        public string ErrorMessage => _errorMessage;
        
        /// <summary>
        /// Gets the severity level of this validation rule.
        /// </summary>
        public ValidationSeverity Severity => _severity;
        
        /// <summary>
        /// Gets the composed rules that make up this validation rule.
        /// </summary>
        public IReadOnlyList<ValidationRule> ComposedRules => _composedRules.AsReadOnly();
        
        /// <summary>
        /// Initializes a new instance of the ValidationRule class.
        /// </summary>
        /// <param name="name">The name of the validation rule</param>
        /// <param name="description">The description of the validation rule</param>
        /// <param name="type">The type of validation</param>
        /// <param name="expression">The validation expression</param>
        /// <param name="errorMessage">The error message for validation failures</param>
        /// <param name="severity">The severity level of the validation</param>
        /// <param name="validationFunc">The validation function (optional)</param>
        public ValidationRule(
            string name,
            string description,
            ValidationType type,
            string expression,
            string errorMessage,
            ValidationSeverity severity,
            Func<object, bool> validationFunc = null)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _description = description ?? throw new ArgumentNullException(nameof(description));
            _type = type;
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            _severity = severity;
            _validationFunc = validationFunc;
        }
        
        /// <summary>
        /// Creates a simple validation rule with a validation function.
        /// </summary>
        /// <param name="name">The name of the validation rule</param>
        /// <param name="description">The description of the validation rule</param>
        /// <param name="validationFunc">The validation function</param>
        /// <param name="errorMessage">The error message for validation failures</param>
        /// <param name="severity">The severity level of the validation (optional)</param>
        /// <returns>A new validation rule</returns>
        public static ValidationRule Create(
            string name,
            string description,
            Func<object, bool> validationFunc,
            string errorMessage,
            ValidationSeverity severity = ValidationSeverity.Error)
        {
            return new ValidationRule(
                name,
                description,
                ValidationType.Custom,
                $"Custom validation: {name}",
                errorMessage,
                severity,
                validationFunc);
        }
        
        /// <summary>
        /// Creates a required field validation rule.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate</param>
        /// <returns>A new required field validation rule</returns>
        public static ValidationRule Required(string propertyName)
        {
            return new ValidationRule(
                $"Required_{propertyName}",
                $"The {propertyName} field is required",
                ValidationType.Required,
                $"!string.IsNullOrWhiteSpace({propertyName})",
                $"The {propertyName} field is required.",
                ValidationSeverity.Error,
                obj => obj != null && !string.IsNullOrWhiteSpace(obj.ToString()));
        }
        
        /// <summary>
        /// Creates a minimum length validation rule.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate</param>
        /// <param name="minLength">The minimum length</param>
        /// <returns>A new minimum length validation rule</returns>
        public static ValidationRule MinLength(string propertyName, int minLength)
        {
            return new ValidationRule(
                $"MinLength_{propertyName}_{minLength}",
                $"The {propertyName} field must be at least {minLength} characters long",
                ValidationType.Length,
                $"{propertyName}.Length >= {minLength}",
                $"The {propertyName} field must be at least {minLength} characters long.",
                ValidationSeverity.Error,
                obj => obj?.ToString()?.Length >= minLength);
        }
        
        /// <summary>
        /// Creates a maximum length validation rule.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate</param>
        /// <param name="maxLength">The maximum length</param>
        /// <returns>A new maximum length validation rule</returns>
        public static ValidationRule MaxLength(string propertyName, int maxLength)
        {
            return new ValidationRule(
                $"MaxLength_{propertyName}_{maxLength}",
                $"The {propertyName} field must be no more than {maxLength} characters long",
                ValidationType.Length,
                $"{propertyName}.Length <= {maxLength}",
                $"The {propertyName} field must be no more than {maxLength} characters long.",
                ValidationSeverity.Error,
                obj => obj?.ToString()?.Length <= maxLength);
        }
        
        /// <summary>
        /// Creates a pattern validation rule.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate</param>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="description">The description of the pattern</param>
        /// <returns>A new pattern validation rule</returns>
        public static ValidationRule Pattern(string propertyName, string pattern, string description)
        {
            return new ValidationRule(
                $"Pattern_{propertyName}",
                $"The {propertyName} field must match the pattern: {description}",
                ValidationType.Pattern,
                $"Regex.IsMatch({propertyName}, \"{pattern}\")",
                $"The {propertyName} field must match the pattern: {description}.",
                ValidationSeverity.Error,
                obj => System.Text.RegularExpressions.Regex.IsMatch(obj?.ToString() ?? "", pattern));
        }
        
        /// <summary>
        /// Validates an object using this validation rule.
        /// </summary>
        /// <param name="target">The object to validate</param>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool Validate(object target)
        {
            // If this is a composite rule, validate all composed rules
            if (_composedRules.Any())
            {
                return _composedRules.All(rule => rule.Validate(target));
            }
            
            // Use the validation function if provided
            if (_validationFunc != null)
            {
                return _validationFunc(target);
            }
            
            // Default validation logic based on type
            return ValidateByType(target);
        }
        
        /// <summary>
        /// Composes this validation rule with other validation rules.
        /// </summary>
        /// <param name="components">The validation rules to compose with</param>
        /// <returns>A new composite validation rule</returns>
        public ValidationRule Compose(params ValidationRule[] components)
        {
            // Include this rule in the composition
            var allComponents = new List<ValidationRule> { this };
            allComponents.AddRange(components);
            
            var compositeName = $"Composite_{string.Join("_", allComponents.Select(c => c.Name))}";
            var compositeDescription = $"Composite rule combining: {string.Join(", ", allComponents.Select(c => c.Name))}";
            var compositeExpression = string.Join(" AND ", allComponents.Select(c => c.Expression));
            var compositeErrorMessage = string.Join("; ", allComponents.Select(c => c.ErrorMessage));
            var compositeSeverity = allComponents.Max(c => c.Severity);
            
            var compositeRule = new ValidationRule(
                compositeName,
                compositeDescription,
                ValidationType.Composite,
                compositeExpression,
                compositeErrorMessage,
                compositeSeverity,
                target => allComponents.All(c => c.Validate(target)));
            
            // Add all components to the composed rules list
            compositeRule._composedRules.AddRange(allComponents);
            
            return compositeRule;
        }
        
        /// <summary>
        /// Determines if this validation rule can be composed with another validation rule.
        /// </summary>
        /// <param name="other">The other validation rule to check</param>
        /// <returns>True if composition is possible, false otherwise</returns>
        public bool CanComposeWith(ValidationRule other) => other != null;
        
        /// <summary>
        /// Decomposes this validation rule into its constituent parts.
        /// </summary>
        /// <returns>An enumerable of the decomposed validation rules</returns>
        public IEnumerable<ValidationRule> Decompose()
        {
            if (_composedRules.Any())
            {
                return _composedRules.SelectMany(rule => rule.Decompose());
            }
            
            return new[] { this };
        }
        
        /// <summary>
        /// Validates an object based on the validation type.
        /// </summary>
        /// <param name="target">The object to validate</param>
        /// <returns>True if validation passes, false otherwise</returns>
        private bool ValidateByType(object target)
        {
            switch (_type)
            {
                case ValidationType.Required:
                    return target != null && !string.IsNullOrWhiteSpace(target.ToString());
                
                case ValidationType.Length:
                    // Length validation requires specific implementation based on expression
                    return true; // Placeholder
                
                case ValidationType.Pattern:
                    // Pattern validation requires specific implementation based on expression
                    return true; // Placeholder
                
                case ValidationType.Custom:
                    // Custom validation requires the validation function
                    return _validationFunc?.Invoke(target) ?? true;
                
                case ValidationType.Composite:
                    // Composite validation is handled by composed rules
                    return _composedRules.All(rule => rule.Validate(target));
                
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Returns a string representation of this validation rule.
        /// </summary>
        /// <returns>A formatted string containing the validation rule details</returns>
        public override string ToString()
        {
            var parts = new List<string>
            {
                $"Name: {Name}",
                $"Type: {Type}",
                $"Severity: {Severity}",
                $"Expression: {Expression}"
            };
            
            if (!string.IsNullOrEmpty(Description))
                parts.Add($"Description: {Description}");
            
            if (!string.IsNullOrEmpty(ErrorMessage))
                parts.Add($"Error: {ErrorMessage}");
            
            if (_composedRules.Any())
                parts.Add($"Composed Rules: {_composedRules.Count}");
            
            return string.Join(" | ", parts);
        }
    }
    
    /// <summary>
    /// Represents the type of validation rule.
    /// </summary>
    public enum ValidationType
    {
        /// <summary>
        /// Required field validation.
        /// </summary>
        Required = 0,
        
        /// <summary>
        /// Length validation (min/max).
        /// </summary>
        Length = 1,
        
        /// <summary>
        /// Pattern validation (regex).
        /// </summary>
        Pattern = 2,
        
        /// <summary>
        /// Custom validation with custom logic.
        /// </summary>
        Custom = 3,
        
        /// <summary>
        /// Composite validation combining multiple rules.
        /// </summary>
        Composite = 4
    }
    
    /// <summary>
    /// Represents the severity level of a validation rule.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information level - for informational purposes only.
        /// </summary>
        Info = 0,
        
        /// <summary>
        /// Warning level - should be reviewed but doesn't prevent operation.
        /// </summary>
        Warning = 1,
        
        /// <summary>
        /// Error level - prevents operation from continuing.
        /// </summary>
        Error = 2,
        
        /// <summary>
        /// Critical level - severe issue that must be addressed immediately.
        /// </summary>
        Critical = 3
    }
} 