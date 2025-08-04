using System.Collections.Generic;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Detailed result for an individual test.
    /// </summary>
    public class DetailedTestResult
    {
        /// <summary>
        /// Unique identifier for this test.
        /// </summary>
        public string TestId { get; set; } = string.Empty;

        /// <summary>
        /// Name of the test.
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Test class name.
        /// </summary>
        public string TestClass { get; set; } = string.Empty;

        /// <summary>
        /// Status of the test.
        /// </summary>
        public TestStatus Status { get; set; }

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Error message if the test failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace if the test failed.
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Test output.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata for this test.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
} 