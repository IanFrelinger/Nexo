namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for codified domain knowledge attached to a brick (federated catalog / portal).
/// </summary>
public sealed class DomainKnowledgeDto
{
    public IReadOnlyList<string> Standards { get; set; } = [];
    public IReadOnlyList<DomainRuleDto> Rules { get; set; } = [];
    public IReadOnlyDictionary<string, string> ReferenceData { get; set; } = new Dictionary<string, string>();
    public IReadOnlyList<LearnedPatternDto> LearnedPatterns { get; set; } = [];
    public long ExecutionCount { get; set; }
}

public sealed class DomainRuleDto
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Pattern { get; set; }
    public string? Severity { get; set; }
    public string? CweId { get; set; }
}

public sealed class LearnedPatternDto
{
    public string Pattern { get; set; } = "";
    public string Outcome { get; set; } = "";
    public double Confidence { get; set; }
    public int OccurrenceCount { get; set; }
}
