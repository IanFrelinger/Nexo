namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Categories for organizing bricks by their primary function.
/// </summary>
public enum BrickCategory
{
    /// <summary>Collects or ingests external data into the workflow.</summary>
    Input,

    /// <summary>Writes or emits results to external systems.</summary>
    Output,

    /// <summary>Inspects data and produces findings or metrics.</summary>
    Analysis,

    /// <summary>Transforms data shape or representation without external side effects.</summary>
    Transform,

    /// <summary>Creates new content, code, or artifacts.</summary>
    Generation,

    /// <summary>Checks constraints, schemas, or policy compliance.</summary>
    Validation,

    /// <summary>Enforces security controls or threat detection.</summary>
    Security,

    /// <summary>Coordinates flow, branching, or orchestration decisions.</summary>
    Control
}
