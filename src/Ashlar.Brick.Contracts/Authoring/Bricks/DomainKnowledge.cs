namespace Ashlar.Core.Domain.Bricks;

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
