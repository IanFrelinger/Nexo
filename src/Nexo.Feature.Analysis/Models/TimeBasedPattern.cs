using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Time-based pattern in test failures.
    /// </summary>
    public class TimeBasedPattern
    {
        /// <summary>
        /// Description of the pattern.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Start time of the pattern.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time of the pattern.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Day of week when this pattern occurs.
        /// </summary>
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Tests affected by this pattern.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();
    }
} 