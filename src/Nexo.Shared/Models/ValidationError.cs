using System;
using System.Collections.Generic;
using Nexo.Shared.Values;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public partial class ValidationError
    {
        public string Message { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string? Field { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public ValidationError(string message, ValidationSeverity? severity = null, string? field = null)
        {
            Message = message;
            Severity = severity ?? ValidationSeverity.Error;
            Field = field;
        }
    }
} 