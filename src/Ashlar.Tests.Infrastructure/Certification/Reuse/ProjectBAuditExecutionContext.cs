using Ashlar.Core.Domain.Execution;

namespace Ashlar.Tests.Infrastructure.Certification.Reuse;

/// <summary>Project b audit execution context.</summary>
internal sealed class ProjectBAuditExecutionContext : IExecutionContext
{
    /// <summary>Agent id.</summary>
    public string AgentId { get; } = "project-b";
    /// <summary>Behavior id.</summary>
    public string BehaviorId { get; } = "certified-brick-smoke";
    /// <summary>Is air gapped.</summary>
    public bool IsAirGapped { get; } = true;
    /// <summary>Audit mode.</summary>
    public bool AuditMode { get; } = true;
    /// <summary>Provider.</summary>
    public string Provider { get; } = "deterministic";
    /// <summary>Variables.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}
