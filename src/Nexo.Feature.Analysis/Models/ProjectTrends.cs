using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Project-specific trend analysis.
    /// </summary>
    public class ProjectTrends
    {
        /// <summary>
        /// Project name.
        /// </summary>
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Success rate trend for this project.
        /// </summary>
        public TrendData SuccessRateTrend { get; set; } = new TrendData();

        /// <summary>
        /// Test count trend for this project.
        /// </summary>
        public TrendData TestCountTrend { get; set; } = new TrendData();

        /// <summary>
        /// Coverage trend for this project.
        /// </summary>
        public TrendData CoverageTrend { get; set; } = new TrendData();
    }
} 