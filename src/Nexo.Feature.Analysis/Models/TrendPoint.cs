using System;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// A single data point in a trend.
    /// </summary>
    public class TrendPoint
    {
        /// <summary>
        /// Timestamp for this data point.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Value at this point in time.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Number of samples used to calculate this value.
        /// </summary>
        public int SampleCount { get; set; }
    }
} 