using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Defines a strategy for repairing failed pipeline artifacts.
    /// </summary>
    /// <typeparam name="TRequest">The type of request for generation</typeparam>
    /// <typeparam name="TArtifact">The type of artifact generated</typeparam>
    public interface IRepairStrategy<TRequest, TArtifact>
    {
        /// <summary>
        /// Attempts to repair a failed generation result based on validation findings and policy outcomes.
        /// </summary>
        /// <param name="request">The original request that generated the artifact</param>
        /// <param name="lastGen">The last generation result that failed</param>
        /// <param name="gateFindings">Validation results from compilation gates</param>
        /// <param name="policy">Policy outcome from policy gates</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>
        /// A tuple containing:
        /// - Applied: Whether the repair was successfully applied
        /// - PatchedSource: The repaired source code
        /// - Note: Description of what was repaired
        /// </returns>
        Task<(bool Applied, string PatchedSource, string Note)> TryRepairAsync(
            TRequest request, 
            GenerationResult<TArtifact> lastGen, 
            IReadOnlyList<ValidationResult> gateFindings, 
            PolicyOutcome? policy, 
            CancellationToken ct);
    }
}
