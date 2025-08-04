using System;
using System.Collections.Generic;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents a validation warning.
    /// </summary>
    public class ValidationWarning
    {
        public string Message { get; set; }
        public string Field { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public ValidationWarning(string message, string field = null)
        {
            Message = message;
            Field = field;
        }
    }
} 