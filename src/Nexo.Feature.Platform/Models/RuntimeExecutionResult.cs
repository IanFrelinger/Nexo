using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents the result of runtime code execution.
    /// </summary>
    public sealed class RuntimeExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
        public TimeSpan Duration { get; set; }
        public IReadOnlyDictionary<string, object> Data { get; set; }

        public RuntimeExecutionResult()
        {
            IsSuccess = false;
            Output = string.Empty;
            Error = string.Empty;
            ExitCode = 0;
            Duration = TimeSpan.Zero;
            Data = new Dictionary<string, object>();
        }
    }
}