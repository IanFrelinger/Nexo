using Nexo.Core.Domain.Execution;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>Shared air-gapped execution context for DepExtract brick tests.</summary>
internal sealed class AirGappedTestContext : IExecutionContext
{
    public string AgentId => "test-agent";
    public string BehaviorId => "dep-extract-test";
    public bool IsAirGapped => true;
    public bool AuditMode => true;
    public string Provider => "none";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}
