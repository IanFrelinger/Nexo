namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// An agentic implementation that uses LLM-based reasoning.
/// </summary>
public class AgenticImplementation
{
    /// <summary>Stable implementation identifier within the brick.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable implementation name.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Short description of agentic behavior.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Executor type (e.g., "LLMChainExecutor").
    /// </summary>
    public string Executor { get; init; } = "LLMChainExecutor";
    
    /// <summary>
    /// LLM configuration.
    /// </summary>
    public LLMConfig LLMConfig { get; init; } = new();
    
    /// <summary>
    /// Provider-specific model mappings.
    /// </summary>
    public IReadOnlyDictionary<string, ProviderConfig> ProviderMappings { get; init; } = 
        new Dictionary<string, ProviderConfig>();
    
    public ImplementationCharacteristics Characteristics { get; init; } = new();
}
