using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Entities.Iteration.Performance
{
    /// <summary>
    /// Performance profile for iteration strategies
    /// </summary>
    public record IterationPerformanceProfile
    {
        /// <summary>
        /// CPU efficiency level
        /// </summary>
        public Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel CpuEfficiency { get; init; } = Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel.Medium;
        
        /// <summary>
        /// Memory efficiency level
        /// </summary>
        public Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel MemoryEfficiency { get; init; } = Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel.Medium;
        
        /// <summary>
        /// Scalability level
        /// </summary>
        public Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel Scalability { get; init; } = Nexo.Core.Domain.Entities.Infrastructure.PerformanceLevel.Medium;
        
        /// <summary>
        /// Minimum optimal data size
        /// </summary>
        public int OptimalDataSizeMin { get; init; } = 0;
        
        /// <summary>
        /// Maximum optimal data size
        /// </summary>
        public int OptimalDataSizeMax { get; init; } = int.MaxValue;
        
        /// <summary>
        /// Whether this strategy supports parallelization
        /// </summary>
        public bool SupportsParallelization { get; init; } = false;
        
        /// <summary>
        /// Whether this strategy requires IList interface
        /// </summary>
        public bool RequiresIList { get; init; } = false;
        
        /// <summary>
        /// Whether this strategy supports async operations
        /// </summary>
        public bool SupportsAsync { get; init; } = false;
        
        /// <summary>
        /// Whether this strategy is suitable for real-time scenarios
        /// </summary>
        public bool SuitableForRealTime { get; init; } = false;
    }

    /// <summary>
    /// Performance estimate for iteration strategy
    /// </summary>
    public record PerformanceEstimate
    {
        /// <summary>
        /// Estimated execution time in milliseconds
        /// </summary>
        public double EstimatedExecutionTimeMs { get; init; }
        
        /// <summary>
        /// Estimated memory usage in MB
        /// </summary>
        public double EstimatedMemoryUsageMB { get; init; }
        
        /// <summary>
        /// Confidence level of the estimate (0-1)
        /// </summary>
        public double Confidence { get; init; }
        
        /// <summary>
        /// Performance score (higher is better)
        /// </summary>
        public double PerformanceScore { get; init; }
        
        /// <summary>
        /// Whether this strategy meets the requirements
        /// </summary>
        public bool MeetsRequirements { get; init; }
    }
}
