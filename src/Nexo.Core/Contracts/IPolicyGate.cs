using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Interface for evaluating artifacts against policies.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact to evaluate</typeparam>
    public interface IPolicyGate<TArtifact>
    {
        /// <summary>
        /// Evaluates an artifact against applicable policies.
        /// </summary>
        /// <param name="artifact">The artifact to evaluate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A policy outcome indicating compliance status</returns>
        Task<PolicyOutcome> EvaluateAsync(TArtifact artifact, CancellationToken ct = default);
    }
}
