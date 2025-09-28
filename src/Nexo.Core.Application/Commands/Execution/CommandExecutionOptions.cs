namespace Nexo.Core.Application.Commands.Execution
{
    /// <summary>
    /// Options for command execution
    /// </summary>
    public class CommandExecutionOptions
    {
        /// <summary>
        /// Whether to continue execution if a command fails
        /// </summary>
        public bool ContinueOnFailure { get; set; } = false;

        /// <summary>
        /// Whether to execute commands in parallel
        /// </summary>
        public bool ExecuteInParallel { get; set; } = false;

        /// <summary>
        /// Maximum number of parallel executions
        /// </summary>
        public int MaxParallelExecutions { get; set; } = 5;

        /// <summary>
        /// Timeout for the entire orchestration
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Whether to validate commands before execution
        /// </summary>
        public bool ValidateBeforeExecution { get; set; } = true;
    }
}
