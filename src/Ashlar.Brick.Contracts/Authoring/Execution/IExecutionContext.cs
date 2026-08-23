namespace Ashlar.Core.Domain.Execution;

/// <summary>
/// Execution context providing environment and runtime information to bricks.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Agent ID executing this behavior.
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// Behavior ID being executed.
    /// </summary>
    string BehaviorId { get; }
    
    /// <summary>
    /// Whether the environment is air-gapped (no network access).
    /// </summary>
    bool IsAirGapped { get; }
    
    /// <summary>
    /// Whether audit mode is enabled (requires deterministic results).
    /// </summary>
    bool AuditMode { get; }
    
    /// <summary>
    /// Current LLM provider (e.g., "openai", "azure", "ollama").
    /// </summary>
    string Provider { get; }
    
    /// <summary>
    /// Variables available in the execution context (from previous steps).
    /// </summary>
    IReadOnlyDictionary<string, object> Variables { get; }
}
