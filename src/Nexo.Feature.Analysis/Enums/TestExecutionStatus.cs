namespace Nexo.Feature.Analysis.Enums
{
    /// <summary>
    /// Status of test execution.
    /// </summary>
    public enum TestExecutionStatus
    {
        /// <summary>
        /// Tests are currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Tests completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Tests failed to execute.
        /// </summary>
        Failed,

        /// <summary>
        /// Tests were cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Tests timed out.
        /// </summary>
        Timeout
    }
} 