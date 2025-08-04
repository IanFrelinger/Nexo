using System;
using System.Collections.Generic;
using Nexo.Feature.Analysis.Enums;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Performance regression analysis.
    /// </summary>
    public class PerformanceRegression
    {
        /// <summary>
        /// Metric that regressed.
        /// </summary>
        public string Metric { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the regression.
        /// </summary>
        public RegressionSeverity Severity { get; set; }

        /// <summary>
        /// Start date of the regression.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// End date of the regression.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Percentage of degradation.
        /// </summary>
        public double DegradationPercentage { get; set; }

        /// <summary>
        /// Tests affected by this regression.
        /// </summary>
        public List<string> AffectedTests { get; set; } = new List<string>();

        /// <summary>
        /// Possible causes of the regression.
        /// </summary>
        public List<string> PossibleCauses { get; set; } = new List<string>();
    }
} 