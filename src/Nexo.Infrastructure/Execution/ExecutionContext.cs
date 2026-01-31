using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Concrete implementation of execution context.
/// </summary>
public class ExecutionContext : IExecutionContext
{
    public string AgentId { get; init; } = string.Empty;
    public string BehaviorId { get; init; } = string.Empty;
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; }
    public string Provider { get; init; } = "openai";
    public Dictionary<string, object> Variables { get; init; } = new();

    IReadOnlyDictionary<string, object> IExecutionContext.Variables => Variables;
}

