using System;

namespace Nexo.Feature.Factory.Domain.Models
{
    /// <summary>
    /// Represents a validation rule that needs to be implemented.
    /// </summary>
    public sealed class ValidationRule
    {
        /// <summary>
        /// Gets the name of the validation rule.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the validation rule.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the validation type.
        /// </summary>
        public ValidationType Type { get; }

        /// <summary>
        /// Gets the validation expression or pattern.
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Gets the error message when validation fails.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the severity of the validation rule.
        /// </summary>
        public ValidationSeverity Severity { get; }

        /// <summary>
        /// Gets the property or field this rule applies to.
        /// </summary>
        public string? AppliesTo { get; }

        /// <summary>
        /// Initializes a new instance of the ValidationRule class.
        /// </summary>
        /// <param name="name">The rule name</param>
        /// <param name="description">The rule description</param>
        /// <param name="type">The validation type</param>
        /// <param name="expression">The validation expression</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="severity">The validation severity</param>
        /// <param name="appliesTo">The property/field this rule applies to</param>
        public ValidationRule(string name, string description, ValidationType type, string expression, string errorMessage, ValidationSeverity severity = ValidationSeverity.Error, string? appliesTo = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Type = type;
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            Severity = severity;
            AppliesTo = appliesTo;
        }
    }

    /// <summary>
    /// Represents the type of validation rule.
    /// </summary>
    public enum ValidationType
    {
        Required,
        StringLength,
        Range,
        Pattern,
        Email,
        Phone,
        Custom
    }

    /// <summary>
    /// Represents the severity of a validation rule.
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
}
