namespace Nexo.Brick.Contracts;

/// <summary>
/// A single codified domain rule attached to a brick's knowledge bundle.
/// </summary>
public sealed class DomainRuleDto
{
    /// <summary>Stable rule identifier within the brick's knowledge bundle.</summary>
    public string Id { get; set; } = "";

    /// <summary>Human-readable description of the rule intent.</summary>
    public string Description { get; set; } = "";

    /// <summary>Optional match pattern (regex, glob, or domain-specific DSL).</summary>
    public string? Pattern { get; set; }

    /// <summary>Severity label (e.g. <c>info</c>, <c>warning</c>, <c>critical</c>).</summary>
    public string? Severity { get; set; }

    /// <summary>Common Weakness Enumeration id when the rule maps to CWE taxonomy.</summary>
    public string? CweId { get; set; }
}
