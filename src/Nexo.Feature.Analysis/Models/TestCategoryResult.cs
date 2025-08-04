namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Aggregated results for a test category.
    /// </summary>
    public class TestCategoryResult
    {
        /// <summary>
        /// Name of the test category.
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Total number of tests in this category.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Number of tests that passed.
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// Number of tests that failed.
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// Number of tests that were skipped.
        /// </summary>
        public int SkippedTests { get; set; }

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Success rate percentage.
        /// </summary>
        public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
    }
} 