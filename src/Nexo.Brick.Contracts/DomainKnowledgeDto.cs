namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for codified domain knowledge attached to a brick (federated catalog / portal).
/// </summary>
public sealed class DomainKnowledgeDto
{
    /// <summary>Industry or internal standards referenced by this brick (e.g. OWASP, PCI).</summary>
    public IReadOnlyList<string> Standards { get; set; } = [];

    /// <summary>Explicit domain rules enforced or surfaced during execution.</summary>
    public IReadOnlyList<DomainRuleDto> Rules { get; set; } = [];

    /// <summary>Lookup tables and reference data keyed by domain concept.</summary>
    public IReadOnlyDictionary<string, string> ReferenceData { get; set; } = new Dictionary<string, string>();

    /// <summary>Patterns learned from prior executions on this or peer nodes.</summary>
    public IReadOnlyList<LearnedPatternDto> LearnedPatterns { get; set; } = [];

    /// <summary>Total executions contributing to learned patterns and stats.</summary>
    public long ExecutionCount { get; set; }
}
