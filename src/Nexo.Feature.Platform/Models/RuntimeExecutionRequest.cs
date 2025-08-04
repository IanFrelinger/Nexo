using System;
using System.Collections.Generic;
using System.IO;

namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a request to execute code in a runtime environment.
    /// </summary>
    public sealed class RuntimeExecutionRequest
    {
        public string Code { get; set; }
        public string WorkingDirectory { get; set; }
        public IReadOnlyDictionary<string, string> EnvironmentVariables { get; set; }
        public int TimeoutMs { get; set; }
        public bool CaptureOutput { get; set; }
        public IReadOnlyDictionary<string, object> Options { get; set; }

        public RuntimeExecutionRequest()
        {
            Code = string.Empty;
            WorkingDirectory = Directory.GetCurrentDirectory();
            EnvironmentVariables = new Dictionary<string, string>();
            TimeoutMs = 30000;
            CaptureOutput = true;
            Options = new Dictionary<string, object>();
        }
    }
}