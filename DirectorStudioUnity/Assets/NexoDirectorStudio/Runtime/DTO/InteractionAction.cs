using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents an action that can be executed in an interaction.
    /// </summary>
    public sealed record InteractionAction
    {
        /// <summary>
        /// Type of action (e.g., "SetVariable", "SpawnObject", "PlayAudio", "ShowDialog").
        /// </summary>
        public string ActionType { get; init; } = string.Empty;
        
        /// <summary>
        /// Parameters for the action.
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Delay in seconds before executing this action.
        /// </summary>
        public float DelaySeconds { get; init; } = 0.0f;
        
        /// <summary>
        /// Whether this action should be executed asynchronously.
        /// </summary>
        public bool IsAsync { get; init; } = false;
    }
}
