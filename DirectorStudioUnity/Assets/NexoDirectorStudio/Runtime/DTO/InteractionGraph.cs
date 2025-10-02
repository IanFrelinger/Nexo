using System;
using System.Collections.Generic;

namespace NexoDirectorStudio.DTO
{
    /// <summary>
    /// Represents the interaction graph for the game slice, including triggers, events, and quests.
    /// This defines how players interact with the world and what happens in response.
    /// </summary>
    public sealed record InteractionGraph
    {
        /// <summary>
        /// Unique identifier for this interaction graph.
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// The world layout this graph was generated from.
        /// </summary>
        public string WorldLayoutId { get; init; } = string.Empty;
        
        /// <summary>
        /// All interaction nodes in the graph.
        /// </summary>
        public IReadOnlyList<InteractionNode> Nodes { get; init; } = Array.Empty<InteractionNode>();
        
        /// <summary>
        /// All connections between nodes.
        /// </summary>
        public IReadOnlyList<InteractionConnection> Connections { get; init; } = Array.Empty<InteractionConnection>();
        
        /// <summary>
        /// Global variables that can be used across interactions.
        /// </summary>
        public IReadOnlyList<InteractionVariable> Variables { get; init; } = Array.Empty<InteractionVariable>();
        
        /// <summary>
        /// Entry points for the interaction graph.
        /// </summary>
        public IReadOnlyList<string> EntryPointIds { get; init; } = Array.Empty<string>();
        
        /// <summary>
        /// Seed used for deterministic generation.
        /// </summary>
        public int Seed { get; init; }
        
        /// <summary>
        /// Timestamp when the graph was generated.
        /// </summary>
        public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}