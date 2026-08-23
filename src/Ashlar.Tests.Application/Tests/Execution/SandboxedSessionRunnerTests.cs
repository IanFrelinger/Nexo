using FluentAssertions;
using Ashlar.Agents.TestKit;
using Ashlar.Core.Application.Execution.Ports;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Execution;

/// <summary>
/// The backend-neutral session port and its TestKit fake (extension spec Part B): tests
/// drive the SAME <see cref="ISandboxedSessionRunner"/> port production uses, teardown is
/// the disposal contract, and use-after-teardown is loud. The Docker backend itself is
/// pinned in <c>Ashlar.Tests.Infrastructure</c> (<c>DockerSandboxSessionTests</c>).
/// </summary>
public sealed class SandboxedSessionRunnerTests
{
    private static SandboxSpec KeepaliveSpec() => new(
        Image: "proposer:latest",
        Mounts: new[] { new Mount("/host/work", "/work", ReadOnly: false) },
        Network: NetworkAccess.None,
        Command: new[] { "sleep", "infinity" },
        Limits: new ResourceLimits(Memory: "2g"));

    [Fact]
    public async Task FakeSessionRunner_RecordsTheFullSessionLifecycle()
    {
        var runner = new FakeSandboxedSessionRunner(
            FakeSandboxedSessionRunner.Failure("build failed"),
            FakeSandboxedSessionRunner.Success("all tests passed"));

        await using (var session = await runner.StartAsync(KeepaliveSpec()))
        {
            (await session.ExecAsync(new[] { "dotnet", "build" })).Succeeded.Should().BeFalse();
            (await session.ExecAsync(new[] { "dotnet", "test" })).Succeeded.Should().BeTrue();
        }

        runner.ActiveSessions.Should().Be(0, "await using guarantees teardown");
        var recorded = runner.Sessions.Should().ContainSingle().Subject;
        recorded.Spec.Image.Should().Be("proposer:latest");
        recorded.ExecCommands.Should().HaveCount(2);
        recorded.ExecCommands[1].Should().ContainInOrder("dotnet", "test");
    }

    [Fact]
    public async Task FakeSession_EnforcesTheSameUseAfterTeardownTeeth_AsTheRealBackend()
    {
        var runner = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success());
        var session = await runner.StartAsync(KeepaliveSpec());
        await session.DisposeAsync();

        var act = () => session.ExecAsync(new[] { "echo", "hi" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
