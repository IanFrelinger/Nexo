using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents the result of runtime code validation.
    /// </summary>
    public sealed class RuntimeValidationResult
    {
        public bool IsValid { get; set; }
        public IReadOnlyList<string> Errors { get; set; }
        public IReadOnlyList<string> Warnings { get; set; }

        public RuntimeValidationResult()
        {
            IsValid = false;
            Errors = new List<string>();
            Warnings = new List<string>();
        }
    }
}