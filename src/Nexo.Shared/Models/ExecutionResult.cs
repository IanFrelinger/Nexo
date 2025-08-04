namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of a command execution.
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Gets or sets whether the execution was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the exit code.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the standard output.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets the error output.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }
}