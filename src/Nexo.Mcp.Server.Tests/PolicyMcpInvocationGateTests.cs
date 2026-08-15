using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Xunit;

namespace Nexo.Mcp.Server.Tests;

public sealed class PolicyMcpInvocationGateTests
{
    private sealed class StubPolicy : IPolicy
    {
        private readonly bool _approve;
        private readonly string _reason;

        public StubPolicy(bool approve, string reason = "nope") => (_approve, _reason) = (approve, reason);

        public bool Approve(ToolCall toolCall, WorldSnapshot s, out string reason)
        {
            reason = _approve ? string.Empty : _reason;
            return _approve;
        }
    }

    private static readonly WorldSnapshot Snapshot = WorldSnapshot.ForRepo("X:/repo");

    private static PolicyMcpInvocationGate Create(params IPolicy[] policies)
        => new(policies, NullLogger<PolicyMcpInvocationGate>.Instance);

    [Fact]
    public void No_registered_policies_allows_the_call()
    {
        var decision = Create().Authorize(new ToolCall("repo.fs.read", Json.Object()), Snapshot);

        decision.Allowed.Should().BeTrue("the allowlist has already constrained reachability");
    }

    [Fact]
    public void All_approving_policies_allow_the_call()
    {
        var gate = Create(new StubPolicy(true), new StubPolicy(true));

        gate.Authorize(new ToolCall("repo.fs.read", Json.Object()), Snapshot).Allowed.Should().BeTrue();
    }

    [Fact]
    public void Any_denying_policy_denies_with_its_reason()
    {
        var gate = Create(new StubPolicy(true), new StubPolicy(false, "write access is not permitted"));

        var decision = gate.Authorize(new ToolCall("repo.fs.write", Json.Object()), Snapshot);

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain("StubPolicy").And.Contain("write access is not permitted");
    }
}
