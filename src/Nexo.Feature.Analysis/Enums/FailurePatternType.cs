namespace Nexo.Feature.Analysis.Enums
{
    /// <summary>
    /// Types of failure patterns.
    /// </summary>
    public enum FailurePatternType
    {
        /// <summary>
        /// Test timeout failures.
        /// </summary>
        Timeout,

        /// <summary>
        /// Resource exhaustion failures.
        /// </summary>
        ResourceExhaustion,

        /// <summary>
        /// Environment-related failures.
        /// </summary>
        EnvironmentIssue,

        /// <summary>
        /// Code-related failures.
        /// </summary>
        CodeIssue,

        /// <summary>
        /// Flaky test failures.
        /// </summary>
        FlakyTest,

        /// <summary>
        /// Dependency-related failures.
        /// </summary>
        DependencyIssue,

        /// <summary>
        /// Configuration-related failures.
        /// </summary>
        ConfigurationIssue
    }
} 