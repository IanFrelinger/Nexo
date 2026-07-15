using Nexo.Core.Domain.Execution;

namespace Nexo.GameDomain;

/// <summary>
/// Minimal <see cref="IExecutionContext"/> for Unity/player hosts.
/// Defaults to air-gapped + audit mode so selectors prefer deterministic execution.
/// </summary>
public sealed class GameplayExecutionContext : IExecutionContext
{
    /// <summary>Creates a gameplay execution context.</summary>
    public GameplayExecutionContext(
        string agentId = "gameplay",
        string behaviorId = "tick",
        bool isAirGapped = true,
        bool auditMode = true,
        IReadOnlyDictionary<string, object>? variables = null)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        BehaviorId = behaviorId ?? throw new ArgumentNullException(nameof(behaviorId));
        IsAirGapped = isAirGapped;
        AuditMode = auditMode;
        Provider = "none";
        Variables = variables ?? new Dictionary<string, object>();
    }

    /// <inheritdoc />
    public string AgentId { get; }

    /// <inheritdoc />
    public string BehaviorId { get; }

    /// <inheritdoc />
    public bool IsAirGapped { get; }

    /// <inheritdoc />
    public bool AuditMode { get; }

    /// <inheritdoc />
    public string Provider { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Variables { get; }

    /// <summary>Context for a named player/controller agent.</summary>
    public static GameplayExecutionContext ForPlayer(string playerId, string behaviorId = "tick")
        => new(agentId: playerId, behaviorId: behaviorId);
}
