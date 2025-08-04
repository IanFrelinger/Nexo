using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Models
{
    /// <summary>
    /// Test coverage information.
    /// </summary>
    public class TestCoverageInfo
    {
        /// <summary>
        /// Overall code coverage percentage.
        /// </summary>
        public double OverallCoverage { get; set; }

        /// <summary>
        /// Line coverage percentage.
        /// </summary>
        public double LineCoverage { get; set; }

        /// <summary>
        /// Branch coverage percentage.
        /// </summary>
        public double BranchCoverage { get; set; }

        /// <summary>
        /// Method coverage percentage.
        /// </summary>
        public double MethodCoverage { get; set; }

        /// <summary>
        /// Coverage by module.
        /// </summary>
        public Dictionary<string, double> CoverageByModule { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// Areas that are not covered by tests.
        /// </summary>
        public List<string> UncoveredAreas { get; set; } = new List<string>();
    }
} 