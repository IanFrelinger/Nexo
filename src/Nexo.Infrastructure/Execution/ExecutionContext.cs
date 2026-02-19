using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Concrete implementation of execution context.
/// </summary>
public class ExecutionContext : IExecutionContext
{
    /// <summary>Identifier for the executing agent.</summary>
    public string AgentId { get; init; } = string.Empty;
    /// <summary>Identifier for the behavior/workflow being executed.</summary>
    public string BehaviorId { get; init; } = string.Empty;
    /// <summary>When true, no network calls (use mock/offline providers only).</summary>
    public bool IsAirGapped { get; init; }
    /// <summary>When true, log decisions for audit trail.</summary>
    public bool AuditMode { get; init; }
    /// <summary>LLM provider name: ollama, openai, azure, mock, offline, video.</summary>
    public string Provider { get; init; } = "openai";
    /// <summary>Key-value variables for this execution (e.g. brick outputs).</summary>
    public Dictionary<string, object> Variables { get; init; } = new();

    IReadOnlyDictionary<string, object> IExecutionContext.Variables => Variables;
}

