using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Performance metrics for test execution.
    /// </summary>
    public class TestPerformanceMetrics
    {
        /// <summary>
        /// Memory usage at the start of test execution.
        /// </summary>
        public long MemoryUsageStartBytes { get; set; }

        /// <summary>
        /// Memory usage at the end of test execution.
        /// </summary>
        public long MemoryUsageEndBytes { get; set; }

        /// <summary>
        /// Peak memory usage during test execution.
        /// </summary>
        public long PeakMemoryUsageBytes { get; set; }

        /// <summary>
        /// Average CPU usage percentage.
        /// </summary>
        public double CpuUsagePercentage { get; set; }

        /// <summary>
        /// Number of parallel test executions.
        /// </summary>
        public int ParallelExecutions { get; set; }

        /// <summary>
        /// Execution time of the slowest test.
        /// </summary>
        public long SlowestTestTimeMs { get; set; }

        /// <summary>
        /// Execution time of the fastest test.
        /// </summary>
        public long FastestTestTimeMs { get; set; }

        /// <summary>
        /// Average test execution time.
        /// </summary>
        public double AverageTestTimeMs { get; set; }

        /// <summary>
        /// List of tests that are considered slow.
        /// </summary>
        public List<string> SlowTests { get; set; } = new List<string>();
    }
} 