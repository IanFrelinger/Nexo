using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Progress
{
    /// <summary>
    /// Interface for reporting test execution progress.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports the start of test execution.
        /// </summary>
        /// <param name="totalTests">Total number of tests to execute</param>
        void ReportTestExecutionStart(int totalTests);

        /// <summary>
        /// Reports the start of a specific test.
        /// </summary>
        /// <param name="testId">The test identifier</param>
        /// <param name="testName">The test name</param>
        /// <param name="testIndex">The current test index (0-based)</param>
        void ReportTestStart(string testId, string testName, int testIndex);

        /// <summary>
        /// Reports the completion of a specific test.
        /// </summary>
        /// <param name="testId">The test identifier</param>
        /// <param name="testName">The test name</param>
        /// <param name="result">The test result</param>
        /// <param name="duration">The test duration</param>
        /// <param name="testIndex">The current test index (0-based)</param>
        void ReportTestComplete(string testId, string testName, bool result, TimeSpan duration, int testIndex);

        /// <summary>
        /// Reports test execution progress.
        /// </summary>
        /// <param name="completedTests">Number of completed tests</param>
        /// <param name="totalTests">Total number of tests</param>
        /// <param name="elapsedTime">Elapsed execution time</param>
        /// <param name="estimatedRemaining">Estimated remaining time</param>
        void ReportProgress(int completedTests, int totalTests, TimeSpan elapsedTime, TimeSpan estimatedRemaining);

        /// <summary>
        /// Reports test execution completion.
        /// </summary>
        /// <param name="summary">The test execution summary</param>
        void ReportTestExecutionComplete(TestExecutionSummary summary);

        /// <summary>
        /// Reports an error during test execution.
        /// </summary>
        /// <param name="testId">The test identifier</param>
        /// <param name="error">The error message</param>
        void ReportError(string testId, string error);

        /// <summary>
        /// Reports a warning during test execution.
        /// </summary>
        /// <param name="testId">The test identifier</param>
        /// <param name="warning">The warning message</param>
        void ReportWarning(string testId, string warning);

        /// <summary>
        /// Reports coverage information.
        /// </summary>
        /// <param name="coverage">The coverage information</param>
        void ReportCoverage(TestCoverageInfo coverage);
    }
}
