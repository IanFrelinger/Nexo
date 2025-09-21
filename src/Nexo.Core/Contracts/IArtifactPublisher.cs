using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Interface for publishing artifacts with validation and policy results.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact to publish</typeparam>
    public interface IArtifactPublisher<TArtifact>
    {
        /// <summary>
        /// Publishes an artifact with associated validation and policy results.
        /// </summary>
        /// <param name="artifact">The artifact to publish</param>
        /// <param name="gates">Validation results from compilation gates</param>
        /// <param name="policy">Policy evaluation outcome</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A pipeline report summarizing the publication</returns>
        Task<PipelineReport> PublishAsync(TArtifact artifact, IReadOnlyList<ValidationResult> gates, PolicyOutcome policy, CancellationToken ct = default);
    }
}
