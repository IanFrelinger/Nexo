using System;
using Nexo.Shared.Enums;

namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public sealed class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string ErrorCode { get; set; }

        public ValidationError(string field, string message, ValidationSeverity severity = ValidationSeverity.Error, string errorCode = "")
        {
            if (string.IsNullOrWhiteSpace(field))
                throw new ArgumentException("Field cannot be null or whitespace", nameof(field));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be null or whitespace", nameof(message));
            Field = field;
            Message = message;
            Severity = severity;
            ErrorCode = errorCode;
        }
    }
}