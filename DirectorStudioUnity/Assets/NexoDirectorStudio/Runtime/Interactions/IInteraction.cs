using System;
using UnityEngine;

namespace NexoDirectorStudio.Interactions
{
    /// <summary>
    /// Interface for all interactive objects in DirectorStudio.
    /// Provides centralized interaction management and metrics.
    /// </summary>
    public interface IInteraction : IInvokeInteraction
    {
        /// <summary>
        /// Event fired when this interaction is triggered
        /// </summary>
        event Action<IInteraction> Triggered;
        
        /// <summary>
        /// Whether this interaction is currently armed and ready to trigger
        /// </summary>
        bool IsArmed { get; }
        
        /// <summary>
        /// Whether this interaction has been triggered at least once
        /// </summary>
        bool HasTriggered { get; }
        
        /// <summary>
        /// Gets the interaction ID for tracking purposes.
        /// </summary>
        string InteractionId { get; }
        
        /// <summary>
        /// Initialize the interaction (called once during registration)
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Arm the interaction for triggering
        /// </summary>
        void Arm();
        
        /// <summary>
        /// Trigger the interaction (can be called by autoplayer or user input)
        /// </summary>
        void Trigger();
        
        /// <summary>
        /// Attempts to invoke this interaction directly.
        /// </summary>
        /// <returns>True if the interaction was successfully triggered</returns>
        bool TryInvoke();
        
        /// <summary>
        /// Get interaction metadata for metrics
        /// </summary>
        InteractionMetadata GetMetadata();
    }
    
    /// <summary>
    /// Metadata about an interaction for metrics and debugging
    /// </summary>
    [Serializable]
    public struct InteractionMetadata
    {
        public string Type;
        public string Name;
        public Vector3 Position;
        public bool IsArmed;
        public bool HasTriggered;
        public float TriggerTime;
        public int TriggerCount;
    }
}
