using System.Collections.Generic;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Seasonal pattern analysis.
    /// </summary>
    public class SeasonalPattern
    {
        /// <summary>
        /// Type of seasonal pattern.
        /// </summary>
        public SeasonalPatternType Type { get; set; }

        /// <summary>
        /// Description of the pattern.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Strength of the pattern (0-1).
        /// </summary>
        public double Strength { get; set; }

        /// <summary>
        /// Metrics affected by this pattern.
        /// </summary>
        public List<string> AffectedMetrics { get; set; } = new List<string>();

        /// <summary>
        /// Time range when this pattern applies.
        /// </summary>
        public TimeRange TimeRange { get; set; } = new TimeRange();
    }
} 