using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts.Capabilities
{
    /// <summary>
    /// Capability interface for plugins that can perform actions.
    /// </summary>
    public interface IAct
    {
        /// <summary>
        /// Gets the name of the action capability.
        /// </summary>
        string CapabilityName { get; }

        /// <summary>
        /// Performs an action based on the given input.
        /// </summary>
        /// <param name="input">The input to act upon</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The action result</returns>
        Task<object> ActAsync(object input, CancellationToken cancellationToken = default);
    }
}
