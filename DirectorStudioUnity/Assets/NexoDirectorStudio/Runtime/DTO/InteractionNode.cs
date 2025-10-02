using System;
using System.Collections.Generic;
using UnityEngine;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents a single interaction node in the graph.
    /// </summary>
    public sealed record InteractionNode
    {
        /// <summary>
        /// Unique identifier for this node.
        /// </summary>
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of interaction node (e.g., "Trigger", "Event", "Quest", "Dialog", "Spawn").
        /// </summary>
        public string NodeType { get; init; } = string.Empty;
        
        /// <summary>
        /// Position in the world where this interaction occurs.
        /// </summary>
        public Vector3 WorldPosition { get; init; }
        
        /// <summary>
        /// Name or title of the interaction.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Description of what this interaction does.
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Conditions that must be met for this interaction to be available.
        /// </summary>
        public IReadOnlyList<InteractionCondition> Conditions { get; init; } = Array.Empty<InteractionCondition>();
        
        /// <summary>
        /// Actions that are executed when this interaction is triggered.
        /// </summary>
        public IReadOnlyList<InteractionAction> Actions { get; init; } = Array.Empty<InteractionAction>();
        
        /// <summary>
        /// Whether this interaction can be repeated.
        /// </summary>
        public bool IsRepeatable { get; init; } = false;
        
        /// <summary>
        /// Priority of this interaction (higher numbers are processed first).
        /// </summary>
        public int Priority { get; init; } = 0;
        
        /// <summary>
        /// Additional properties specific to the node type.
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
}
