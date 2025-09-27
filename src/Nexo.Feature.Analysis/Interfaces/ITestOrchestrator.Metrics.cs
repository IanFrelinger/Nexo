using System;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Metrics and utilization classes
    /// </summary>
    public partial class ParallelExecutionMetrics
    {
        /// <summary>
        /// Maximum parallelism achieved.
        /// </summary>
        public int MaxParallelism { get; set; }

        /// <summary>
        /// Average parallelism during execution.
        /// </summary>
        public double AverageParallelism { get; set; }

        /// <summary>
        /// Total execution time.
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Sequential execution time (for comparison).
        /// </summary>
        public TimeSpan SequentialTime { get; set; }

        /// <summary>
        /// Speedup factor compared to sequential execution.
        /// </summary>
        public double SpeedupFactor { get; set; }

        /// <summary>
        /// Resource utilization efficiency.
        /// </summary>
        public double Efficiency { get; set; }
    }

    /// <summary>
    /// Resource utilization information.
    /// </summary>
    public partial class ResourceUtilization
    {
        /// <summary>
        /// Current CPU usage percentage.
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Current memory usage in MB.
        /// </summary>
        public double MemoryUsageMB { get; set; }

        /// <summary>
        /// Available memory in MB.
        /// </summary>
        public double AvailableMemoryMB { get; set; }

        /// <summary>
        /// Number of available CPU cores.
        /// </summary>
        public int AvailableCores { get; set; }

        /// <summary>
        /// Recommended maximum parallelism.
        /// </summary>
        public int RecommendedMaxParallelism { get; set; }

        /// <summary>
        /// Whether resources are constrained.
        /// </summary>
        public bool IsResourceConstrained { get; set; }
    }
}
