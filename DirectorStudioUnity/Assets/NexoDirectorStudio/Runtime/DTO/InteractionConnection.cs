using System;
using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a connection between two interaction nodes.
    /// </summary>
    public sealed record InteractionConnection
    {
        /// <summary>
        /// Unique identifier for this connection.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// ID of the source node.
        /// </summary>
        public string SourceNodeId { get; init; } = string.Empty;
        
        /// <summary>
        /// ID of the target node.
        /// </summary>
        public string TargetNodeId { get; init; } = string.Empty;
        
        /// <summary>
        /// Type of connection (e.g., "Success", "Failure", "Timeout", "Condition").
        /// </summary>
        public string ConnectionType { get; init; } = string.Empty;
        
        /// <summary>
        /// Conditions that must be met for this connection to be active.
        /// </summary>
        public IReadOnlyList<InteractionCondition> Conditions { get; init; } = Array.Empty<InteractionCondition>();
        
        /// <summary>
        /// Weight of this connection (for random selection).
        /// </summary>
        public float Weight { get; init; } = 1.0f;
    }
}
