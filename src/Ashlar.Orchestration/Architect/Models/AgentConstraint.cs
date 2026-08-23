using System.Text.Json;
using Ashlar.Abstractions.Execution;

namespace Ashlar.Orchestration.Architect.Models;

/// <summary>
/// Represents a constraint that an agent must satisfy.
/// 
/// Contains:
/// - Constraint type (Performance, Security, Compatibility, etc.)
/// - Human-readable description
/// - Whether the constraint is mandatory or optional
/// 
/// Used to specify requirements that agents must meet during execution.
/// </summary>
public sealed record AgentConstraint
{
    /// <summary>
    /// Type of constraint (e.g., "Performance", "Security", "Compatibility").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Constraint description or requirement.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this constraint is mandatory or optional.
    /// </summary>
    public bool IsMandatory { get; init; } = true;
}
