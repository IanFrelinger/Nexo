using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts.Capabilities
{
    /// <summary>
    /// Capability interface for plugins that can sense or observe the environment.
    /// </summary>
    public interface ISense
    {
        /// <summary>
        /// Gets the name of the sensing capability.
        /// </summary>
        string CapabilityName { get; }

        /// <summary>
        /// Performs sensing operation on the given input.
        /// </summary>
        /// <param name="input">The input to sense</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The sensed data</returns>
        Task<object> SenseAsync(object input, CancellationToken cancellationToken = default);
    }
}
