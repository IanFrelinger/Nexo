using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces.Core
{
    /// <summary>
    /// Interface for managing pipeline resources and allocation.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Allocates resources for pipeline execution.
        /// </summary>
        /// <param name="requirements">The resource requirements.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Resource allocation result.</returns>
        Task<ResourceAllocation> AllocateResourcesAsync(
            ResourceRequirements requirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        /// <param name="allocationId">The allocation identifier.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Task representing the release operation.</returns>
        Task ReleaseResourcesAsync(
            string allocationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current resource utilization.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Current resource utilization.</returns>
        Task<ResourceUtilization> GetCurrentUtilizationAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes resource allocation based on current usage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>Resource optimization recommendations.</returns>
        Task<ResourceOptimizationRecommendation> OptimizeAllocationAsync(
            CancellationToken cancellationToken = default);
    }
}
