using FluentAssertions;
using Nexo.Agents.TestKit;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Execution.Sandbox;
using Nexo.Infrastructure.Scaling;
using Xunit;

namespace Nexo.Tests.Application.Tests.Execution;

/// <summary>
/// Resource attestation and session provenance (extension spec Part B): the backend attests
/// what the environment actually is, an environment weaker than requested is refused, the
/// environment becomes hash-identified certificate inputs, and every session lifecycle
/// transition emits a provenance event whose sink can never fail the operation.
/// </summary>
public sealed class SessionAttestationTests
{
    private static SandboxSpec Spec(ResourceLimits? limits = null) => new(
        Image: "proposer:latest",
        Mounts: Array.Empty<Mount>(),
        Network: NetworkAccess.None,
        Command: new[] { "sleep", "infinity" },
        Limits: limits);

    private static SessionAttestation Attestation(
        ResourceLimits requested,
        long? memory = null,
        long? pids = null,
        long? nano = null,
        string? digest = "sha256:abc") => new()
    {
        SessionId = "nexo-session-t",
        Image = "proposer:latest",
        ImageDigest = digest,
        EngineVersion = "27.0",
        Requested = requested,
        EffectiveMemoryBytes = memory,
        EffectivePidsLimit = pids,
        EffectiveNanoCpus = nano,
        AttestedAt = DateTimeOffset.UnixEpoch,
    };

    // --- parsing -------------------------------------------------------------------------

    [Theory]
    [InlineData("2g", 2L * 1024 * 1024 * 1024)]
    [InlineData("512m", 512L * 1024 * 1024)]
    [InlineData("64k", 64L * 1024)]
    [InlineData("1024", 1024L)]
    [InlineData("junk", null)]
    [InlineData("", null)]
    [InlineData("-5m", null)]
    public void ParseMemoryToBytes_HandlesBackendEncodings(string value, long? expected) =>
        SessionAttestation.ParseMemoryToBytes(value).Should().Be(expected);

    [Theory]
    [InlineData("2", 2_000_000_000L)]
    [InlineData("0.5", 500_000_000L)]
    [InlineData("x", null)]
    public void ParseCpusToNano_HandlesBackendEncodings(string value, long? expected) =>
        SessionAttestation.ParseCpusToNano(value).Should().Be(expected);

    [Fact]
    public void ParseInspectLine_ReadsDigestAndCaps_AndTreatsNilAsUnknown()
    {
        DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t2147483648\t256\t2000000000")
            .Should().Be(("sha256:abc", 2147483648L, 256L, 2000000000L));
        DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t0\t<nil>\t0")
            .Should().Be(("sha256:abc", 0L, (long?)null, 0L));
    }

    // --- shortfall verdicts --------------------------------------------------------------

    [Fact]
    public void FaithfulEnvironment_HasNoShortfalls()
    {
        var attestation = Attestation(
            new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2"),
            memory: 2L * 1024 * 1024 * 1024, pids: 256, nano: 2_000_000_000);

        attestation.FindShortfalls().Should().BeEmpty();
        var act = attestation.ThrowIfBelowRequested;
        act.Should().NotThrow();
    }

    [Fact]
    public void WeakerOrUnverifiedLimits_AreShortfalls_AndStricterOnesAreNot()
    {
        var requested = new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2");

        // Weaker in every dimension: bigger memory cap, unlimited pids, more cpu.
        Attestation(requested, memory: 4L * 1024 * 1024 * 1024, pids: 0, nano: 4_000_000_000)
            .FindShortfalls().Should().HaveCount(3);
        // Unknown values are unverified, which is fail-closed a shortfall.
        Attestation(requested, memory: null, pids: null, nano: null)
            .FindShortfalls().Should().HaveCount(3);
        // Stricter than requested is never a shortfall.
        Attestation(requested, memory: 1L * 1024 * 1024 * 1024, pids: 128, nano: 1_000_000_000)
            .FindShortfalls().Should().BeEmpty();
    }

    [Fact]
    public void UnparsableRequestedLimit_IsAShortfall_NotATrustedGuess()
    {
        Attestation(new ResourceLimits(Memory: "lots"), memory: 123456789)
            .FindShortfalls().Should().ContainSingle().Which.Should().Contain("unverifiable");
    }

    [Fact]
    public void NothingRequested_MeansNothingToVerify()
    {
        Attestation(new ResourceLimits()).FindShortfalls().Should().BeEmpty();
    }

    [Fact]
    public void ThrowIfBelowRequested_NamesEveryShortfall()
    {
        var attestation = Attestation(new ResourceLimits(Memory: "2g", Pids: 10), memory: null, pids: null);

        var act = attestation.ThrowIfBelowRequested;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*refusing*memory*")
            .WithMessage("*pids*");
    }

    // --- docker attestation flow ---------------------------------------------------------

    [Fact]
    public async Task AttestAsync_ReadsInspectAndEngineVersion_AndEmitsProvenance()
    {
        var sink = new RecordingSink();
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "run" => new ProcessCommandResult(0, "cid", ""),
            "inspect" => new ProcessCommandResult(0, "sha256:deadbeef\t2147483648\t256\t2000000000\n", ""),
            "version" => new ProcessCommandResult(0, "27.1.1\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch), provenance: sink);
        var session = await runner.StartAsync(Spec(new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2")));

        var attestation = await session.AttestAsync();

        attestation.ImageDigest.Should().Be("sha256:deadbeef");
        attestation.EngineVersion.Should().Be("27.1.1");
        attestation.FindShortfalls().Should().BeEmpty();
        sink.Events.Select(e => e.Outcome).Should().ContainInOrder(
            SessionProvenanceOutcomes.Started, SessionProvenanceOutcomes.Attested);
    }

    [Fact]
    public async Task AttestAsync_FailsClosed_WhenInspectFails()
    {
        var docker = new RoutedProcessRunner(args => args[0] == "inspect"
            ? new ProcessCommandResult(1, "", "no such container")
            : new ProcessCommandResult(0, "", ""));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));
        var session = await runner.StartAsync(Spec());

        var act = () => session.AttestAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot be attested*");
    }

    // --- provenance lifecycle ------------------------------------------------------------

    [Fact]
    public async Task SessionLifecycle_EmitsStartedAndStopped_AndAThrowingSinkNeverFailsTheOperation()
    {
        var sink = new RecordingSink { ThrowOnRecord = true };
        var docker = new RoutedProcessRunner(_ => new ProcessCommandResult(0, "", ""));
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch), provenance: sink);

        var act = async () =>
        {
            var session = await runner.StartAsync(Spec());
            await session.DisposeAsync();
        };

        await act.Should().NotThrowAsync("a provenance sink failure must never fail a session operation");
    }

    [Fact]
    public async Task Reaper_EmitsReapedEvents()
    {
        var sink = new RecordingSink();
        var docker = new RoutedProcessRunner(args => args[0] == "ps"
            ? new ProcessCommandResult(0, "nexo-session-old\t1\n", "")
            : new ProcessCommandResult(0, "", ""));
        var reaper = new DockerSandboxSessionReaper(
            docker, new FrozenClock(DateTimeOffset.FromUnixTimeSeconds(100)), provenance: sink);

        await reaper.SweepAsync();

        sink.Events.Should().ContainSingle().Which.Should().Match<SessionProvenanceEvent>(e =>
            e.Outcome == SessionProvenanceOutcomes.Reaped && e.SessionId == "nexo-session-old");
    }

    // --- certificate inputs --------------------------------------------------------------

    [Fact]
    public void EnvironmentInputs_CarryDigestSpecHashAndAttestationHash_Deterministically()
    {
        var spec = Spec(new ResourceLimits(Memory: "2g"));
        var attestation = Attestation(new ResourceLimits(Memory: "2g"), memory: 2L * 1024 * 1024 * 1024);

        var first = SessionEnvironmentInputs.From(spec, attestation);
        var second = SessionEnvironmentInputs.From(spec, attestation);

        first.Select(i => i.Kind).Should().Equal(
            SessionEnvironmentInputs.ImageDigestKind,
            SessionEnvironmentInputs.SandboxSpecKind,
            SessionEnvironmentInputs.AttestationKind);
        first[0].Hash.Should().Be("sha256:abc");
        first.Should().OnlyContain(i => !string.IsNullOrWhiteSpace(i.Hash));
        second.Select(i => i.Hash).Should().Equal(first.Select(i => i.Hash),
            "environment inputs must be deterministic for the same spec + attestation");
    }

    [Fact]
    public void EnvironmentInputs_RefuseAnUnresolvedImageIdentity()
    {
        var act = () => SessionEnvironmentInputs.From(
            Spec(), Attestation(new ResourceLimits(), digest: null));

        act.Should().Throw<InvalidOperationException>().WithMessage("*image digest*");
    }

    // --- TestKit fake --------------------------------------------------------------------

    [Fact]
    public async Task FakeSession_AttestsFaithfullyByDefault_AndHonorsOverrides()
    {
        var runner = new FakeSandboxedSessionRunner(FakeSandboxedSessionRunner.Success());
        var session = (FakeSandboxedSession)await runner.StartAsync(
            Spec(new ResourceLimits(Memory: "2g", Pids: 64, Cpus: "1")));

        var faithful = await session.AttestAsync();
        faithful.FindShortfalls().Should().BeEmpty("the default fake environment honors the request");

        session.AttestationOverride = faithful with { EffectiveMemoryBytes = null };
        var weakened = await session.AttestAsync();
        weakened.FindShortfalls().Should().ContainSingle();
    }

    // --- helpers -------------------------------------------------------------------------

    private sealed class RecordingSink : ISessionProvenanceSink
    {
        public List<SessionProvenanceEvent> Events { get; } = new();
        public bool ThrowOnRecord { get; init; }

        public void Record(SessionProvenanceEvent provenanceEvent)
        {
            Events.Add(provenanceEvent);
            if (ThrowOnRecord)
                throw new InvalidOperationException("sink deliberately failing");
        }
    }

    private sealed class FrozenClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FrozenClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class RoutedProcessRunner : IProcessCommandRunner
    {
        private readonly Func<IReadOnlyList<string>, ProcessCommandResult> _handler;

        public RoutedProcessRunner(Func<IReadOnlyList<string>, ProcessCommandResult> handler) =>
            _handler = handler;

        public Task<ProcessCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_handler(arguments));
    }
}
