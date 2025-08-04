using System;
using System.Collections.Generic;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of a build operation.
    /// </summary>
    public class BuildResult
    {
        public bool IsSuccess { get; set; }
        public string Output { get; set; }
        public string Errors { get; set; }
        public long ExecutionTimeMs { get; set; }
        public List<string> OutputFiles { get; set; } = new List<string>();
    }
}