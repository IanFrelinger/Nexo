using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

internal sealed class StateTransitionReplayExecutionContext : IExecutionContext
{
    public string AgentId { get; } = "state-transition-replay";
    public string BehaviorId { get; } = "certified-behavior-replay";
    public bool IsAirGapped { get; } = true;
    public bool AuditMode { get; } = true;
    public string Provider { get; } = "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}
