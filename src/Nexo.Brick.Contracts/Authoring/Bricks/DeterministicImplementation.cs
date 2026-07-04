namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// A deterministic implementation that uses rule-based or pattern-matching logic.
/// </summary>
public class DeterministicImplementation
{
    /// <summary>Stable implementation identifier within the brick.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable implementation name.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Short description of deterministic behavior.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Executor type (e.g., "SemgrepExecutor", "TemplateExecutor", "RuleEngineExecutor").
    /// </summary>
    public string Executor { get; init; } = default!;
    
    /// <summary>
    /// Configuration for the executor.
    /// </summary>
    public IReadOnlyDictionary<string, object> Config { get; init; } = 
        new Dictionary<string, object>();
    
    public ImplementationCharacteristics Characteristics { get; init; } = new();
}
