using System.Text.Json.Serialization;
using UnityEngine;

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
        public string Id { get; init; } = Guid.NewGuid().ToString();
        
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
    
    /// <summary>
    /// Represents a condition for an interaction.
    /// </summary>
    public sealed record InteractionCondition
    {
        /// <summary>
        /// Type of condition (e.g., "VariableEquals", "ItemInInventory", "TimeElapsed").
        /// </summary>
        public string ConditionType { get; init; } = string.Empty;
        
        /// <summary>
        /// Parameters for the condition.
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Whether this condition must be true or false.
        /// </summary>
        public bool MustBeTrue { get; init; } = true;
    }
    
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
    
    /// <summary>
    /// Represents a variable that can be used in interactions.
    /// </summary>
    public sealed record InteractionVariable
    {
        /// <summary>
        /// Name of the variable.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Type of the variable (e.g., "Int", "Float", "String", "Bool").
        /// </summary>
        public string VariableType { get; init; } = string.Empty;
        
        /// <summary>
        /// Initial value of the variable.
        /// </summary>
        public object? InitialValue { get; init; }
        
        /// <summary>
        /// Whether this variable is global (accessible from all interactions).
        /// </summary>
        public bool IsGlobal { get; init; } = true;
        
        /// <summary>
        /// Description of what this variable represents.
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}
