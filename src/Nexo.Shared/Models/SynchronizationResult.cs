using Nexo.Core.Application.Enums;
using Nexo.Core.Application.Interfaces;
using System.Collections.Generic;

namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of a file synchronization operation.
    /// </summary>
    public class SynchronizationResult
    {
        public bool IsSuccess { get; set; }
        public int FilesSynchronized { get; set; }
        public int FilesFailed { get; set; }
        public string ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
        public List<string> SynchronizedFiles { get; set; } = new List<string>();
        public List<string> FailedFiles { get; set; } = new List<string>();
    }
}