using System;
using System.Collections.Generic;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Test execution plan classes
    /// </summary>
    public partial class TestExecutionPlan
    {
        /// <summary>
        /// Ordered phases of test execution.
        /// </summary>
        public List<TestExecutionPhase> Phases { get; set; } = new List<TestExecutionPhase>();

        /// <summary>
        /// Total number of tests in the plan.
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// Estimated execution time.
        /// </summary>
        public TimeSpan EstimatedExecutionTime { get; set; }

        /// <summary>
        /// Dependency graph visualization.
        /// </summary>
        public string DependencyGraph { get; set; } = string.Empty;

        /// <summary>
        /// Whether the plan is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors if any.
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Phase of test execution.
    /// </summary>
    public partial class TestExecutionPhase
    {
        /// <summary>
        /// Phase identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Phase name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Test files in this phase.
        /// </summary>
        public List<string> TestFiles { get; set; } = new List<string>();

        /// <summary>
        /// Whether tests in this phase can run in parallel.
        /// </summary>
        public bool CanRunInParallel { get; set; } = true;

        /// <summary>
        /// Dependencies for this phase.
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Estimated execution time for this phase.
        /// </summary>
        public TimeSpan EstimatedTime { get; set; }
    }
}
