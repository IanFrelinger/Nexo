using Ashlar.Core.Domain.Execution;

namespace Ashlar.Agents.TestKit;

/// <summary>
/// A settable <see cref="IExecutionContext"/> for tests.
///
/// Every property is an init accessor with a sane default, so a test states only
/// the axis it actually cares about — <c>new FakeExecutionContext { AuditMode = true }</c>
/// — instead of re-declaring the whole interface to vary one flag. That is what
/// the hand-rolled copies of this class were doing: five identical shapes that
/// differed only in their default strings.
/// </summary>
public sealed class FakeExecutionContext : IExecutionContext
{
    /// <summary>Agent identifier. Default <c>test-agent</c>.</summary>
    public string AgentId { get; init; } = "test-agent";

    /// <summary>Behavior identifier. Default <c>test-behavior</c>.</summary>
    public string BehaviorId { get; init; } = "test-behavior";

    /// <summary>Whether the context forbids network egress. Default false.</summary>
    public bool IsAirGapped { get; init; }

    /// <summary>
    /// Whether audit policy applies. Default false — the permissive case. Set true
    /// to assert that audit mode forces human review on model-strategy provenance.
    /// </summary>
    public bool AuditMode { get; init; }

    /// <summary>Provider name. Default <c>mock</c>.</summary>
    public string Provider { get; init; } = "mock";

    /// <summary>Workflow variables visible to the brick. Default empty.</summary>
    public IReadOnlyDictionary<string, object> Variables { get; init; } =
        new Dictionary<string, object>();
}
