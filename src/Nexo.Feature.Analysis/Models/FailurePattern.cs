using System.Collections.Generic;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Pattern of test failures.
    /// </summary>
    public class FailurePattern
    {
        /// <summary>
        /// Name of the failure pattern.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of failure pattern.
        /// </summary>
        public FailurePatternType Type { get; set; }

        /// <summary>
        /// Tests affected by this pattern.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();

        /// <summary>
        /// Frequency of this pattern.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Confidence level in this pattern (0-1).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Suggested fixes for this pattern.
        /// </summary>
        public List<string> SuggestedFixes { get; set; } = new List<string>();
    }
} 