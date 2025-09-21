using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Contracts.Capabilities
{
    /// <summary>
    /// Capability interface for plugins that can make decisions based on input.
    /// </summary>
    public interface IDecide
    {
        /// <summary>
        /// Gets the name of the decision capability.
        /// </summary>
        string CapabilityName { get; }

        /// <summary>
        /// Makes a decision based on the given input.
        /// </summary>
        /// <param name="input">The input to make a decision about</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The decision result</returns>
        Task<object> DecideAsync(object input, CancellationToken cancellationToken = default);
    }
}
