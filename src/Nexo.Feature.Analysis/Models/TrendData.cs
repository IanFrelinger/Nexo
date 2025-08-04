using System.Collections.Generic;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Trend analysis data.
    /// </summary>
    public class TrendData
    {
        /// <summary>
        /// Data points in the trend.
        /// </summary>
        public List<TrendPoint> DataPoints { get; set; } = new List<TrendPoint>();

        /// <summary>
        /// Direction of the trend.
        /// </summary>
        public TrendDirection Direction { get; set; }

        /// <summary>
        /// Strength of the trend (0-1).
        /// </summary>
        public double Strength { get; set; }

        /// <summary>
        /// Average value across all data points.
        /// </summary>
        public double AverageValue { get; set; }

        /// <summary>
        /// Minimum value in the trend.
        /// </summary>
        public double MinValue { get; set; }

        /// <summary>
        /// Maximum value in the trend.
        /// </summary>
        public double MaxValue { get; set; }

        /// <summary>
        /// Standard deviation of values.
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// Percentage change from start to end.
        /// </summary>
        public double PercentageChange { get; set; }
    }
} 