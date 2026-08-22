using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// A user-composed workflow created in the visual composer.
/// </summary>
public class WorkflowDefinition
{
    /// <summary>Stable workflow definition identifier.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Display name shown in the visual composer.</summary>
    public string Name { get; init; } = "Untitled Workflow";

    /// <summary>Optional description of the composed workflow.</summary>
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

    /// <summary>UTC timestamp when the workflow was first created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp of the most recent workflow edit.</summary>
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;
}
