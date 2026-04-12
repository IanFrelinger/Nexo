using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Domain.Workflows;

/// <summary>
/// A user-composed workflow created in the visual composer.
/// </summary>
public class WorkflowDefinition
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "Untitled Workflow";
    public string? Description { get; init; }
    
    /// <summary>
    /// All nodes in the workflow (agents, bricks, transforms, I/O).
    /// </summary>
    public IReadOnlyList<WorkflowNode> Nodes { get; init; } = [];
    
    /// <summary>
    /// Connections between node ports.
    /// </summary>
    public IReadOnlyList<VisualWorkflowConnection> Connections { get; init; } = [];
    
    /// <summary>
    /// Global implementation mode for the workflow.
    /// </summary>
    public ImplementationMode DefaultMode { get; init; } = ImplementationMode.Auto;
    
    /// <summary>
    /// Metadata for the visual composer (positions, zoom, etc.).
    /// </summary>
    public WorkflowMetadata Metadata { get; init; } = new();
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A node in the workflow graph.
/// </summary>
public abstract record WorkflowNode
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public Position Position { get; init; } = new(0, 0);
    
    /// <summary>
    /// Input ports that can receive connections.
    /// </summary>
    public IReadOnlyList<NodePort> Inputs { get; init; } = [];
    
    /// <summary>
    /// Output ports that can send data to other nodes.
    /// </summary>
    public IReadOnlyList<NodePort> Outputs { get; init; } = [];
}

/// <summary>
/// An agent node in the workflow.
/// </summary>
public record AgentNode : WorkflowNode
{
    /// <summary>
    /// Reference to the agent definition.
    /// </summary>
    public string AgentId { get; init; } = "";
    
    /// <summary>
    /// Implementation mode for this agent.
    /// </summary>
    public ImplementationMode Mode { get; init; } = ImplementationMode.Auto;
    
    /// <summary>
    /// Per-behavior implementation overrides.
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationMode> BehaviorOverrides { get; init; } 
        = new Dictionary<string, ImplementationMode>();
    
    /// <summary>
    /// Per-brick implementation overrides (within behaviors).
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationType> BrickOverrides { get; init; } 
        = new Dictionary<string, ImplementationType>();
    
    /// <summary>
    /// Parameter values for this agent instance.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } 
        = new Dictionary<string, object>();
}

/// <summary>
/// A standalone brick node (not part of an agent).
/// </summary>
public record BrickNode : WorkflowNode
{
    public string BrickId { get; init; } = "";
    public ImplementationType Implementation { get; init; } = ImplementationType.Auto;
    public string? ProviderOverride { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } 
        = new Dictionary<string, object>();
}

/// <summary>
/// A cluster node (pre-built combination of agents/bricks).
/// </summary>
public record ClusterNode : WorkflowNode
{
    public string ClusterId { get; init; } = "";
    public ImplementationMode Mode { get; init; } = ImplementationMode.Auto;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } 
        = new Dictionary<string, object>();
}

/// <summary>
/// Input node for providing data to the workflow.
/// </summary>
public record InputNode : WorkflowNode
{
    public InputType Type { get; init; } = InputType.File;
    public string? FilePath { get; init; }
    public string? Content { get; init; }
    public string? WebhookUrl { get; init; }
    public string? DatabaseConnectionString { get; init; }
    public string? Query { get; init; }
}

/// <summary>
/// Output node for receiving workflow results.
/// </summary>
public record OutputNode : WorkflowNode
{
    public OutputType Type { get; init; } = OutputType.Display;
    public OutputFormat Format { get; init; } = OutputFormat.Json;
    public string? FilePath { get; init; }
    public string? WebhookUrl { get; init; }
    public string? DatabaseConnectionString { get; init; }
    public string? TableName { get; init; }
}

/// <summary>
/// Transform node for data manipulation.
/// </summary>
public record TransformNode : WorkflowNode
{
    public TransformOperation Operation { get; init; } = TransformOperation.Map;
    public string Expression { get; init; } = "";
}

/// <summary>
/// Conditional node for branching logic.
/// </summary>
public record ConditionalNode : WorkflowNode
{
    public string Condition { get; init; } = "";
}

/// <summary>
/// A port on a node for connecting to other nodes.
/// </summary>
public record NodePort
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Name { get; init; } = "";
    public PortDirection Direction { get; init; }
    public string DataType { get; init; } = "any";
    public bool Required { get; init; } = true;
    public bool AllowMultiple { get; init; } = false;
}

/// <summary>
/// A connection between two ports in a visual workflow.
/// </summary>
public record VisualWorkflowConnection
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string FromNodeId { get; init; } = "";
    public string FromPortId { get; init; } = "";
    public string ToNodeId { get; init; } = "";
    public string ToPortId { get; init; } = "";
    public ConnectionType Type { get; init; } = ConnectionType.Data;
}

public record Position(double X, double Y);

public record WorkflowMetadata
{
    public double Zoom { get; init; } = 1.0;
    public Position ViewportCenter { get; init; } = new(0, 0);
    public IReadOnlyDictionary<string, object> CustomData { get; init; } 
        = new Dictionary<string, object>();
}

/// <summary>
/// Implementation mode for workflows and agents.
/// </summary>
public enum ImplementationMode
{
    Auto,              // Let the system decide per-brick
    DeterministicOnly, // Force all bricks to ⚙️
    AgenticPreferred,  // Prefer 🤖 where available
    Mixed              // Use per-node/per-brick settings
}

public enum InputType { File, Content, Webhook, Database }
public enum OutputType { Display, File, Webhook, Database }
public enum OutputFormat { Json, Xml, Csv, Markdown, Html, Pdf }
public enum TransformOperation { Map, Filter, Reduce, Sort, GroupBy, Merge }
public enum PortDirection { Input, Output }
