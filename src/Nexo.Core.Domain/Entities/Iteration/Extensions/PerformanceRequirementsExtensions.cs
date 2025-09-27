using System;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Entities.Iteration.Requirements;

namespace Nexo.Core.Domain.Entities.Iteration.Extensions
{
    /// <summary>
    /// Extension methods for PerformanceRequirements
    /// </summary>
    public static class PerformanceRequirementsExtensions
    {
        /// <summary>
        /// Convert PerformanceRequirements to IterationRequirements
        /// </summary>
        public static IterationRequirements ToIterationRequirements(this Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements performanceRequirements)
        {
            return new IterationRequirements
            {
                PrioritizeCpu = performanceRequirements.RequiresRealTime,
                PrioritizeMemory = performanceRequirements.MemoryCritical,
                RequiresParallelization = performanceRequirements.PreferParallel,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                Timeout = TimeSpan.FromMilliseconds(performanceRequirements.MaxExecutionTimeMs)
            };
        }
    }
}
