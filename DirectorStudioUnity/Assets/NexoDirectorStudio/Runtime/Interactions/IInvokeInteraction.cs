using UnityEngine;

namespace NexoDirectorStudio.Interactions
{
    /// <summary>
    /// Simple interface for directly invoking interactions without UI events.
    /// Used by smoke tests and automated systems.
    /// </summary>
    public interface IInvokeInteraction
    {
        /// <summary>
        /// Attempts to invoke this interaction directly.
        /// </summary>
        /// <returns>True if the interaction was successfully triggered</returns>
        bool TryInvoke();
        
        /// <summary>
        /// Gets the interaction ID for tracking purposes.
        /// </summary>
        string InteractionId { get; }
    }
}
