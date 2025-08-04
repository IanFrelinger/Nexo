using System;
using System.Collections.Generic;
using Nexo.Shared.Enums;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public class ValidationError
    {
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Field { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public ValidationError(string message, ValidationSeverity severity = ValidationSeverity.Error, string field = null)
        {
            Message = message;
            Severity = severity;
            Field = field;
        }
    }
} 