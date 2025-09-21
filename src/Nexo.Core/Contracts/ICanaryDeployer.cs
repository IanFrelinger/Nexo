using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Defines a strategy for canary deployment of pipeline artifacts.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact to deploy</typeparam>
    public interface ICanaryDeployer<TArtifact>
    {
        /// <summary>
        /// Deploys an artifact to a canary environment for validation.
        /// </summary>
        /// <param name="artifact">The artifact to deploy</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>
        /// A tuple containing:
        /// - Healthy: Whether the canary deployment is healthy
        /// - CanaryId: Unique identifier for the canary deployment
        /// - Note: Description of the canary deployment status
        /// </returns>
        Task<(bool Healthy, string CanaryId, string Note)> CanaryAsync(
            TArtifact artifact, 
            CancellationToken ct);
    }
}
