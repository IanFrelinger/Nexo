using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Group of related tests.
    /// </summary>
    public class TestGroup
    {
        /// <summary>
        /// Name of the test group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Tests in this group.
        /// </summary>
        public List<string> Tests { get; set; } = new List<string>();

        /// <summary>
        /// Failure rate for this group.
        /// </summary>
        public double FailureRate { get; set; }

        /// <summary>
        /// Common error messages in this group.
        /// </summary>
        public List<string> CommonErrors { get; set; } = new List<string>();
    }
} 