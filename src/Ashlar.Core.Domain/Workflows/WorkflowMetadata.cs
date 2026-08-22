namespace Ashlar.Core.Domain.Workflows;

/// <summary>Visual composer state persisted with a <see cref="WorkflowDefinition"/>.</summary>
public record WorkflowMetadata
{
    /// <summary>Current canvas zoom level (1.0 = 100%).</summary>
    public double Zoom { get; init; } = 1.0;

    /// <summary>Center point of the visible viewport.</summary>
    public Position ViewportCenter { get; init; } = new(0, 0);

    /// <summary>Extension bag for composer-specific UI state.</summary>
    public IReadOnlyDictionary<string, object> CustomData { get; init; }
        = new Dictionary<string, object>();
}
