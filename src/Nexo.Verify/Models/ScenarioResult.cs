using System.Collections.Generic;

namespace Nexo.Verify.Models
{
    /// <summary>
    /// Represents the result of executing a scenario
    /// </summary>
    public sealed class ScenarioResult
    {
        /// <summary>
        /// Whether the scenario completed successfully
        /// </summary>
        public bool Completed { get; init; }
        
        /// <summary>
        /// Base directory where output files were written
        /// </summary>
        public string OutputDir { get; init; } = "";
        
        /// <summary>
        /// Log messages from the execution
        /// </summary>
        public List<string> Logs { get; } = new();
        
        /// <summary>
        /// Metrics collected during execution (e.g., execution time, retry count)
        /// </summary>
        public Dictionary<string, double> Metrics { get; } = new();
        
        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? ErrorMessage { get; init; }
        
        /// <summary>
        /// Exit code from the execution
        /// </summary>
        public int ExitCode { get; init; }

        /// <summary>
        /// Read output file as bytes
        /// </summary>
        public byte[] OutputBytes(string relativePath) =>
            File.ReadAllBytes(Path.Combine(OutputDir, relativePath));

        /// <summary>
        /// Read output file as text
        /// </summary>
        public string OutputText(string relativePath) =>
            File.ReadAllText(Path.Combine(OutputDir, relativePath));
    }
}
