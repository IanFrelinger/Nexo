namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Codified domain expertise that lives inside a brick.
/// This is what makes bricks valuable and reusable.
/// </summary>
public class DomainKnowledge
{
    /// <summary>
    /// Industry standards this brick implements (e.g., "OWASP Top 10 2023", "CVSS 3.1").
    /// </summary>
    public IReadOnlyList<string> Standards { get; init; } = [];
    
    /// <summary>
    /// Codified rules and detection patterns.
    /// </summary>
    public IReadOnlyList<DomainRule> Rules { get; init; } = [];
    
    /// <summary>
    /// Reference data paths (lookup tables, configs, templates).
    /// </summary>
    public IReadOnlyDictionary<string, string> ReferenceData { get; init; } = 
        new Dictionary<string, string>();
    
    /// <summary>
    /// Patterns learned from past usage across projects.
    /// </summary>
    public IReadOnlyList<LearnedPattern> LearnedPatterns { get; init; } = [];
    
    /// <summary>
    /// Total number of times this brick has been executed (for learning).
    /// </summary>
    public long ExecutionCount { get; set; }
}

/// <summary>
/// A domain rule that codifies detection logic or validation criteria.
/// </summary>
public class DomainRule
{
    public string Id { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string? Pattern { get; init; }
    public string? Severity { get; init; }
    public string? CweId { get; init; }
    
    public DomainRule(string id, string description, string? pattern = null, string? severity = null, string? cweId = null)
    {
        Id = id;
        Description = description;
        Pattern = pattern;
        Severity = severity;
        CweId = cweId;
    }
}

/// <summary>
/// A pattern learned from production usage that helps improve accuracy.
/// </summary>
public class LearnedPattern
{
    public string Pattern { get; init; } = default!;
    public string Outcome { get; init; } = default!;
    public double Confidence { get; init; }
    public int OccurrenceCount { get; init; }
    
    public LearnedPattern(string pattern, string outcome, double confidence, int occurrenceCount)
    {
        Pattern = pattern;
        Outcome = outcome;
        Confidence = confidence;
        OccurrenceCount = occurrenceCount;
    }
}
