using Nexo.Core.Domain.Execution;

namespace Nexo.Tests.Infrastructure.Certification.Reuse;

internal sealed class ProjectBAuditExecutionContext : IExecutionContext
{
    public string AgentId { get; } = "project-b";
    public string BehaviorId { get; } = "certified-brick-smoke";
    public bool IsAirGapped { get; } = true;
    public bool AuditMode { get; } = true;
    public string Provider { get; } = "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}
