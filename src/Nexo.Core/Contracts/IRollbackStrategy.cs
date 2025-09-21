using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts
{
    /// <summary>
    /// Defines a strategy for rolling back failed pipeline artifacts.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact to rollback</typeparam>
    public interface IRollbackStrategy<TArtifact>
    {
        /// <summary>
        /// Rolls back a failed artifact to a previous stable state.
        /// </summary>
        /// <param name="artifact">The artifact to rollback</param>
        /// <param name="reason">The reason for the rollback</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A description of the rollback operation performed</returns>
        Task<string> RollbackAsync(
            TArtifact artifact, 
            string reason, 
            CancellationToken ct);
    }
}
