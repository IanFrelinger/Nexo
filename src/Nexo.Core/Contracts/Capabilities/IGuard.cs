using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts.Capabilities
{
    /// <summary>
    /// Capability interface for plugins that can guard or validate operations.
    /// </summary>
    public interface IGuard
    {
        /// <summary>
        /// Gets the name of the guard capability.
        /// </summary>
        string CapabilityName { get; }

        /// <summary>
        /// Validates or guards the given input.
        /// </summary>
        /// <param name="input">The input to guard</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the input passes the guard, false otherwise</returns>
        Task<bool> GuardAsync(object input, CancellationToken cancellationToken = default);
    }
}
