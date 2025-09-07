using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Represents a validation error with detailed information about what went wrong.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets the error message describing what went wrong.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// Gets the property or field that caused the validation error.
        /// </summary>
        public string? Property { get; }
        
        /// <summary>
        /// Gets the error code that can be used for programmatic handling.
        /// </summary>
        public string? Code { get; }
        
        /// <summary>
        /// Gets the timestamp when this error was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }
        
        /// <summary>
        /// Initializes a new instance of the ValidationError class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="property">The property that caused the error (optional)</param>
        /// <param name="code">The error code (optional)</param>
        public ValidationError(string message, string? property = null, string? code = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Property = property;
            Code = code;
            Timestamp = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Creates a validation error for a specific property.
        /// </summary>
        /// <param name="property">The property that caused the error</param>
        /// <param name="message">The error message</param>
        /// <param name="code">The error code (optional)</param>
        /// <returns>A new validation error</returns>
        public static ValidationError ForProperty(string property, string message, string? code = null)
        {
            return new ValidationError(message, property, code);
        }
        
        /// <summary>
        /// Creates a validation error with a specific code.
        /// </summary>
        /// <param name="code">The error code</param>
        /// <param name="message">The error message</param>
        /// <param name="property">The property that caused the error (optional)</param>
        /// <returns>A new validation error</returns>
        public static ValidationError WithCode(string code, string message, string? property = null)
        {
            return new ValidationError(message, property, code);
        }
        
        /// <summary>
        /// Returns a string representation of this validation error.
        /// </summary>
        /// <returns>A formatted string containing the error details</returns>
        public override string ToString()
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Property))
                parts.Add($"Property: {Property}");
            
            if (!string.IsNullOrEmpty(Code))
                parts.Add($"Code: {Code}");
            
            parts.Add($"Message: {Message}");
            
            return string.Join(" | ", parts);
        }
        
        /// <summary>
        /// Determines if this validation error equals another validation error.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the errors are equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is ValidationError other)
            {
                return string.Equals(Message, other.Message, StringComparison.Ordinal) &&
                       string.Equals(Property, other.Property, StringComparison.Ordinal) &&
                       string.Equals(Code, other.Code, StringComparison.Ordinal);
            }
            return false;
        }
        
        /// <summary>
        /// Gets the hash code for this validation error.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Message, Property, Code);
        }
    }
} 