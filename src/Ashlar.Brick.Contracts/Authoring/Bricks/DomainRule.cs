namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// A domain rule that codifies detection logic or validation criteria.
/// </summary>
public class DomainRule
{
    /// <summary>Stable rule identifier within the brick's knowledge bundle.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable description of the rule intent.</summary>
    public string Description { get; init; } = default!;

    /// <summary>Optional match pattern (regex, glob, or domain-specific DSL).</summary>
    public string? Pattern { get; init; }

    /// <summary>Severity label (e.g. <c>info</c>, <c>warning</c>, <c>critical</c>).</summary>
    public string? Severity { get; init; }

    /// <summary>Common Weakness Enumeration id when the rule maps to CWE taxonomy.</summary>
    public string? CweId { get; init; }

    /// <summary>Defines a codified domain rule.</summary>
    /// <param name="id">Stable rule identifier.</param>
    /// <param name="description">Human-readable rule description.</param>
    /// <param name="pattern">Optional match pattern.</param>
    /// <param name="severity">Optional severity label.</param>
    /// <param name="cweId">Optional CWE identifier.</param>
    public DomainRule(string id, string description, string? pattern = null, string? severity = null, string? cweId = null)
    {
        Id = id;
        Description = description;
        Pattern = pattern;
        Severity = severity;
        CweId = cweId;
    }
}
