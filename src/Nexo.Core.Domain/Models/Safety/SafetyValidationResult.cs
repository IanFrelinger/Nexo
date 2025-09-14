using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Safety
{
    /// <summary>
    /// Result of safety validation for generated code
    /// </summary>
    public class SafetyValidationResult
    {
        /// <summary>
        /// Whether the code passed safety validation
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Whether the code generation should be blocked
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Whether the code requires human review
        /// </summary>
        public bool RequiresHumanReview { get; set; }

        /// <summary>
        /// List of safety issues found
        /// </summary>
        public List<SafetyIssue> Issues { get; set; } = new List<SafetyIssue>();

        /// <summary>
        /// Overall safety score (0-10)
        /// </summary>
        public double SafetyScore { get; set; }

        /// <summary>
        /// Additional recommendations for improving safety
        /// </summary>
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}
