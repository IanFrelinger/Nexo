namespace Nexo.Shared.Models
{
    /// <summary>
    /// Represents the result of test execution.
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Gets or sets whether all tests passed.
        /// </summary>
        public bool AllTestsPassed { get; set; }

        /// <summary>
        /// Gets or sets the total number of tests.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Gets or sets the number of passed tests.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Gets or sets the number of failed tests.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Gets or sets the test output.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }
}