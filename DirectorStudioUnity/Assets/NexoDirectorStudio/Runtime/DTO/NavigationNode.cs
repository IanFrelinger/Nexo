using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a navigation node in the world.
    /// </summary>
    public sealed record NavigationNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// World position of the node.
        /// </summary>
        public Vector3 Position { get; init; }
        
        /// <summary>
        /// Type of navigation node (e.g., "Waypoint", "Checkpoint", "Spawn", "Goal").
        /// </summary>
        public string NodeType { get; init; } = string.Empty;
        
        /// <summary>
        /// Connected node IDs.
        /// </summary>
        public IReadOnlyList<string> ConnectedNodeIds { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Additional properties specific to the node type.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
}
