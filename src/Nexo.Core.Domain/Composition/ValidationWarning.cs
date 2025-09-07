using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Represents a validation warning with detailed information about potential issues.
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Gets the warning message describing the potential issue.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the property or field that caused the validation warning.
        /// </summary>
        public string Property { get; }
        
        /// <summary>
        /// Gets the warning code that can be used for programmatic handling.
        /// </summary>
        public string Code { get; }
        
        /// <summary>
        /// Gets the timestamp when this warning was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }
        
        /// <summary>
        /// Gets the severity level of this warning.
        /// </summary>
        public WarningSeverity Severity { get; }
        
        /// <summary>
        /// Initializes a new instance of the ValidationWarning class.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="property">The property that caused the warning (optional)</param>
        /// <param name="code">The warning code (optional)</param>
        /// <param name="severity">The severity level of the warning (optional)</param>
        public ValidationWarning(string message, string property = null, string code = null, WarningSeverity severity = WarningSeverity.Medium)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Property = property;
            Code = code;
            Severity = severity;
            Timestamp = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Creates a validation warning for a specific property.
        /// </summary>
        /// <param name="property">The property that caused the warning</param>
        /// <param name="message">The warning message</param>
        /// <param name="code">The warning code (optional)</param>
        /// <param name="severity">The severity level of the warning (optional)</param>
        /// <returns>A new validation warning</returns>
        public static ValidationWarning ForProperty(string property, string message, string code = null, WarningSeverity severity = WarningSeverity.Medium)
        {
            return new ValidationWarning(message, property, code, severity);
        }
        
        /// <summary>
        /// Creates a validation warning with a specific code.
        /// </summary>
        /// <param name="code">The warning code</param>
        /// <param name="message">The warning message</param>
        /// <param name="property">The property that caused the warning (optional)</param>
        /// <param name="severity">The severity level of the warning (optional)</param>
        /// <returns>A new validation warning</returns>
        public static ValidationWarning WithCode(string code, string message, string property = null, WarningSeverity severity = WarningSeverity.Medium)
        {
            return new ValidationWarning(message, property, code, severity);
        }
        
        /// <summary>
        /// Creates a high severity validation warning.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="property">The property that caused the warning (optional)</param>
        /// <param name="code">The warning code (optional)</param>
        /// <returns>A new high severity validation warning</returns>
        public static ValidationWarning High(string message, string property = null, string code = null)
        {
            return new ValidationWarning(message, property, code, WarningSeverity.High);
        }
        
        /// <summary>
        /// Creates a medium severity validation warning.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="property">The property that caused the warning (optional)</param>
        /// <param name="code">The warning code (optional)</param>
        /// <returns>A new medium severity validation warning</returns>
        public static ValidationWarning Medium(string message, string property = null, string code = null)
        {
            return new ValidationWarning(message, property, code, WarningSeverity.Medium);
        }
        
        /// <summary>
        /// Creates a low severity validation warning.
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="property">The property that caused the warning (optional)</param>
        /// <param name="code">The warning code (optional)</param>
        /// <returns>A new low severity validation warning</returns>
        public static ValidationWarning Low(string message, string property = null, string code = null)
        {
            return new ValidationWarning(message, property, code, WarningSeverity.Low);
        }
        
        /// <summary>
        /// Returns a string representation of this validation warning.
        /// </summary>
        /// <returns>A formatted string containing the warning details</returns>
        public override string ToString()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Property))
                parts.Add($"Property: {Property}");
            
            if (!string.IsNullOrEmpty(Code))
                parts.Add($"Code: {Code}");
            
            parts.Add($"Severity: {Severity}");
            parts.Add($"Message: {Message}");
            
            return string.Join(" | ", parts);
        }
        
        /// <summary>
        /// Determines if this validation warning equals another validation warning.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the warnings are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is ValidationWarning other)
            {
                return string.Equals(Message, other.Message, StringComparison.Ordinal) &&
                       string.Equals(Property, other.Property, StringComparison.Ordinal) &&
                       string.Equals(Code, other.Code, StringComparison.Ordinal) &&
                       Severity == other.Severity;
            }
            return false;
        }
        
        /// <summary>
        /// Gets the hash code for this validation warning.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Message, Property, Code, Severity);
        }
    }
    
    /// <summary>
    /// Represents the severity level of a validation warning.
    /// </summary>
    public enum WarningSeverity
    {
        /// <summary>
        /// Low severity warning - informational only.
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// Medium severity warning - should be reviewed.
        /// </summary>
        Medium = 1,
        
        /// <summary>
        /// High severity warning - should be addressed.
        /// </summary>
        High = 2
    }
} 