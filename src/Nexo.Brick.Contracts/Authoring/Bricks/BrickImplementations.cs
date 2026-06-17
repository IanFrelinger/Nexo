namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Available implementations for a brick (deterministic and/or agentic).
/// </summary>
public class BrickImplementations
{
    public DeterministicImplementation? Deterministic { get; init; }
    public AgenticImplementation? Agentic { get; init; }
    
    public bool HasDeterministic => Deterministic is not null;
    public bool HasAgentic => Agentic is not null;
}

/// <summary>
/// A deterministic implementation that uses rule-based or pattern-matching logic.
/// </summary>
public class DeterministicImplementation
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
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

/// <summary>
/// An agentic implementation that uses LLM-based reasoning.
/// </summary>
public class AgenticImplementation
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
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

/// <summary>
/// LLM configuration for agentic implementations.
/// </summary>
public class LLMConfig
{
    public string Model { get; init; } = "gpt-4";
    public string SystemPrompt { get; init; } = default!;
    public double Temperature { get; init; } = 0.1;
    public int MaxTokens { get; init; } = 2000;
    public IReadOnlyList<string>? Tools { get; init; }
}

/// <summary>
/// Provider-specific configuration for model selection.
/// </summary>
public class ProviderConfig
{
    public string Model { get; init; } = default!;
    public string? Endpoint { get; init; }
    
    public ProviderConfig(string model, string? endpoint = null)
    {
        Model = model;
        Endpoint = endpoint;
    }
}

/// <summary>
/// Characteristics of an implementation (latency, determinism, resource usage).
/// </summary>
public class ImplementationCharacteristics
{
    public string Latency { get; init; } = "< 1s";
    public bool Deterministic { get; init; }
    public bool RequiresNetwork { get; init; }
    public ResourceUsage ResourceUsage { get; init; } = ResourceUsage.Low;
}
