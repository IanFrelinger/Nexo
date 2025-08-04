using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Time range for seasonal patterns.
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        /// Start time of the range.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time of the range.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Days of the week when this pattern applies.
        /// </summary>
        public List<DayOfWeek> DaysOfWeek { get; set; } = new List<DayOfWeek>();

        /// <summary>
        /// Months when this pattern applies.
        /// </summary>
        public List<int> Months { get; set; } = new List<int>();
    }
} 