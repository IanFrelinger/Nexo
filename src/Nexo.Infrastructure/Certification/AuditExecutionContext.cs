using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification;

internal sealed class AuditExecutionContext : IExecutionContext
{
    /// <summary>Agent id.</summary>
    public string AgentId => "cert-gate";
    /// <summary>Behavior id.</summary>
    public string BehaviorId => "cert-gate";
    /// <summary>Is air gapped.</summary>
    public bool IsAirGapped => true;
    /// <summary>Audit mode.</summary>
    public bool AuditMode => true;
    /// <summary>Provider.</summary>
    public string Provider => "deterministic";
    /// <summary>Variables.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; } =
        new Dictionary<string, object>();
}
