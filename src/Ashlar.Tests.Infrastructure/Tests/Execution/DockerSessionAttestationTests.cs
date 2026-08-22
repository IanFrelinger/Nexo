using FluentAssertions;
using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Execution.Sandbox;
using Ashlar.Infrastructure.Scaling;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Docker attestation flow, session provenance, and certificate environment inputs
/// (extension spec Part B): the backend attests what the environment actually is
/// fail-closed, every lifecycle transition emits a provenance event whose sink can never
/// fail the operation, and the environment projects into deterministic certificate
/// inputs. (The <c>SessionAttestation</c> model's parsing/shortfall semantics are pinned
/// in <c>Ashlar.Tests.Application</c>.)
/// </summary>
public sealed class DockerSessionAttestationTests
{
    private static SandboxSpec Spec(ResourceLimits? limits = null) => new(
        Image: "proposer:latest",
        Mounts: Array.Empty<Mount>(),
        Network: NetworkAccess.None,
        Command: new[] { "sleep", "infinity" },
        Limits: limits);

    private static SessionAttestation Attestation(string? digest = "sha256:abc") => new()
    {
        SessionId = "ashlar-session-t",
        Image = "proposer:latest",
        ImageDigest = digest,
        EngineVersion = "27.0",
        Requested = new ResourceLimits(Memory: "2g"),
        EffectiveMemoryBytes = 2L * 1024 * 1024 * 1024,
        AttestedAt = DateTimeOffset.UnixEpoch,
    };

    /// <summary>What a hardened session's inspect line looks like: digest, caps, then containment.</summary>
    private const string HardenedInspectLine =
        "sha256:deadbeef\t2147483648\t256\t2000000000\tnone\ttrue\tALL\tno-new-privileges\n";

    [Fact]
    public void ParseInspectLine_ReadsDigestAndCaps_AndTreatsNilAsUnknown()
    {
        var full = DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t2147483648\t256\t2000000000");
        full.ImageDigest.Should().Be("sha256:abc");
        full.MemoryBytes.Should().Be(2147483648L);
        full.PidsLimit.Should().Be(256L);
        full.NanoCpus.Should().Be(2000000000L);

        var nil = DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t0\t<nil>\t0");
        nil.MemoryBytes.Should().Be(0L);
        nil.PidsLimit.Should().BeNull();
        nil.NanoCpus.Should().Be(0L);
    }

    [Fact]
    public void ParseInspectLine_ReadsContainment_AndTreatsMissingFieldsAsUnknown()
    {
        var hardened = DockerSandboxedSessionRunner.ParseInspectLine(HardenedInspectLine);
        hardened.NetworkMode.Should().Be("none");
        hardened.ReadOnlyRootFilesystem.Should().BeTrue();
        hardened.DroppedCapabilities.Should().Equal("ALL");
        hardened.SecurityOptions.Should().Equal("no-new-privileges");

        // An older engine/format that stops after the caps: nothing is inferred.
        var legacy = DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t0\t0\t0");
        legacy.NetworkMode.Should().BeNull("absent evidence is never read as contained");
        legacy.ReadOnlyRootFilesystem.Should().BeNull();
        legacy.DroppedCapabilities.Should().BeEmpty();
        legacy.SecurityOptions.Should().BeEmpty();

        // Several capabilities / options come back as lists; blanks are dropped.
        var many = DockerSandboxedSessionRunner.ParseInspectLine(
            "sha256:abc\t0\t0\t0\tbridge\tfalse\tNET_RAW,SYS_ADMIN\tno-new-privileges,seccomp=unconfined");
        many.DroppedCapabilities.Should().Equal("NET_RAW", "SYS_ADMIN");
        many.SecurityOptions.Should().Equal("no-new-privileges", "seccomp=unconfined");
        many.ReadOnlyRootFilesystem.Should().BeFalse();
    }

    [Fact]
    public void InspectFormat_AsksTheEngineForTheContainmentItApplied()
    {
        DockerSandboxedSessionRunner.InspectFormat.Should().Contain("{{.HostConfig.NetworkMode}}");
        DockerSandboxedSessionRunner.InspectFormat.Should().Contain("{{.HostConfig.ReadonlyRootfs}}");
        DockerSandboxedSessionRunner.InspectFormat.Should().Contain(".HostConfig.CapDrop");
        DockerSandboxedSessionRunner.InspectFormat.Should().Contain(".HostConfig.SecurityOpt");
        DockerSandboxedSessionRunner.InspectFormat.Should().StartWith("{{.Image}}", "the digest capture is unchanged");

        DockerSandboxedSessionRunner.InspectFormat.Should().NotContain(
            "join ",
            "the template's join requires []string and hard-fails the whole inspect when the CLI decodes "
            + "the field as []interface{} -- which is what a container created by an older CLI than its "
            + "daemon reports. A template error throws before ParseInspectLine can read it fail-closed, "
            + "so an unattestable session crashed the loop instead of refusing one iteration.");
    }

    [Fact]
    public void ParseInspectLine_ReadsTheBracketedListsDockerActuallyPrints()
    {
        // Go renders a string slice as "[a b]" whether the CLI typed it as []string or fell back to
        // []interface{}; both print identically, which is why the parse reads the rendered form.
        var hardened = DockerSandboxedSessionRunner.ParseInspectLine(
            "sha256:abc\t0\t0\t0\tnone\ttrue\t[ALL]\t[no-new-privileges]");
        hardened.DroppedCapabilities.Should().Equal("ALL");
        hardened.SecurityOptions.Should().Equal("no-new-privileges");

        var several = DockerSandboxedSessionRunner.ParseInspectLine(
            "sha256:abc\t0\t0\t0\tbridge\tfalse\t[NET_RAW SYS_ADMIN]\t[no-new-privileges seccomp=unconfined]");
        several.DroppedCapabilities.Should().Equal("NET_RAW", "SYS_ADMIN");
        several.SecurityOptions.Should().Equal("no-new-privileges", "seccomp=unconfined");

        // An empty slice prints as "[]" and must read as "nothing dropped", not as a phantom entry.
        var empty = DockerSandboxedSessionRunner.ParseInspectLine("sha256:abc\t0\t0\t0\tnone\ttrue\t[]\t[]");
        empty.DroppedCapabilities.Should().BeEmpty();
        empty.SecurityOptions.Should().BeEmpty();
    }

    [Fact]
    public async Task AttestAsync_ReadsInspectAndEngineVersion_AndEmitsProvenance()
    {
        var sink = new RecordingSink();
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "run" => new ProcessCommandResult(0, "cid", ""),
            "inspect" => new ProcessCommandResult(0, HardenedInspectLine, ""),
            "version" => new ProcessCommandResult(0, "27.1.1\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch), provenance: sink);
        var session = await runner.StartAsync(Spec(new ResourceLimits(Memory: "2g", Pids: 256, Cpus: "2")));

        var attestation = await session.AttestAsync();

        attestation.ImageDigest.Should().Be("sha256:deadbeef");
        attestation.EngineVersion.Should().Be("27.1.1");
        attestation.FindShortfalls().Should().BeEmpty();
        attestation.EffectiveNetworkMode.Should().Be("none");
        attestation.EffectiveReadOnlyRootFilesystem.Should().BeTrue();
        attestation.EffectiveDroppedCapabilities.Should().Equal("ALL");
        attestation.EffectiveSecurityOptions.Should().Equal("no-new-privileges");
        sink.Events.Select(e => e.Outcome).Should().ContainInOrder(
            SessionProvenanceOutcomes.Started, SessionProvenanceOutcomes.Attested);
    }

    [Theory]
    [InlineData("bridge")]
    [InlineData("")]
    public async Task AttestAsync_RefusesAnEngineThatDidNotHonorNoNetwork(string reportedMode)
    {
        // The spec asked for no network. An engine reporting bridge — or nothing at all —
        // has not demonstrably honored it, and the same fail-closed rule as the resource
        // caps applies: unverified is not contained.
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "inspect" => new ProcessCommandResult(0, $"sha256:deadbeef\t0\t0\t0\t{reportedMode}\ttrue\tALL\t\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));
        var session = await runner.StartAsync(Spec());

        var act = () => session.AttestAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*requested no network*");
    }

    [Fact]
    public async Task AttestAsync_DoesNotDemandNoNetwork_WhenTheSpecAskedForEgress()
    {
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "inspect" => new ProcessCommandResult(0, "sha256:deadbeef\t0\t0\t0\tbridge\ttrue\tALL\t\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));
        var session = await runner.StartAsync(Spec() with { Network = NetworkAccess.Unrestricted });

        var attestation = await session.AttestAsync();

        attestation.EffectiveNetworkMode.Should().Be("bridge", "recorded honestly, not refused: egress was requested");
    }

    // --- digest pin -----------------------------------------------------------------------

    [Fact]
    public async Task DigestPin_RefusesToStart_WhenTheImageResolvesToAnotherIdentity()
    {
        var calls = new List<IReadOnlyList<string>>();
        var docker = new RoutedProcessRunner(args =>
        {
            calls.Add(args.ToArray());
            return args[0] == "image"
                ? new ProcessCommandResult(0, "sha256:retagged\n", "")
                : new ProcessCommandResult(0, "", "");
        });
        var runner = new DockerSandboxedSessionRunner(
            docker, new FrozenClock(DateTimeOffset.UnixEpoch), expectedImageDigest: "sha256:pinned");

        var act = () => runner.StartAsync(Spec());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sha256:retagged*sha256:pinned*");
        calls.Should().ContainSingle().Which.Should().ContainInOrder("image", "inspect", "proposer:latest");
        calls.Should().NotContain(c => c[0] == "run", "a refused pin never starts a container");
    }

    [Fact]
    public async Task DigestPin_RefusesToStart_WhenTheImageCannotBeResolved()
    {
        // No image locally (and --pull never would not fetch one): nothing to compare, so
        // nothing to start. "Probably the pinned image" is not a pin.
        var docker = new RoutedProcessRunner(args => args[0] == "image"
            ? new ProcessCommandResult(1, "", "Error: No such image: proposer:latest")
            : new ProcessCommandResult(0, "", ""));
        var runner = new DockerSandboxedSessionRunner(
            docker, new FrozenClock(DateTimeOffset.UnixEpoch), expectedImageDigest: "sha256:pinned");

        var act = () => runner.StartAsync(Spec());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cannot be resolved*");
    }

    [Fact]
    public async Task DigestPin_StartsAndAttests_WhenTheImageMatches_AndRechecksTheRunningContainer()
    {
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "image" => new ProcessCommandResult(0, "sha256:pinned\n", ""),
            "inspect" => new ProcessCommandResult(0, "sha256:pinned\t0\t0\t0\tnone\ttrue\tALL\tno-new-privileges\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(
            docker, new FrozenClock(DateTimeOffset.UnixEpoch), expectedImageDigest: " sha256:pinned ");

        var session = await runner.StartAsync(Spec());
        var attestation = await session.AttestAsync();

        runner.ExpectedImageDigest.Should().Be("sha256:pinned", "the pin is trimmed, not compared raw");
        attestation.ImageDigest.Should().Be("sha256:pinned");
    }

    [Fact]
    public async Task DigestPin_RefusesAtAttestation_WhenTheStartedContainerRunsAnotherImage()
    {
        // The pre-start probe passed, but the tag was retargeted before the container
        // started (or the engine lied): the running container's own image is what counts.
        var docker = new RoutedProcessRunner(args => args[0] switch
        {
            "image" => new ProcessCommandResult(0, "sha256:pinned\n", ""),
            "inspect" => new ProcessCommandResult(0, "sha256:swapped\t0\t0\t0\tnone\ttrue\tALL\t\n", ""),
            _ => new ProcessCommandResult(0, "", ""),
        });
        var runner = new DockerSandboxedSessionRunner(
            docker, new FrozenClock(DateTimeOffset.UnixEpoch), expectedImageDigest: "sha256:pinned");
        var session = await runner.StartAsync(Spec());

        var act = () => session.AttestAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*sha256:swapped*pinned*");
    }

    [Fact]
    public async Task NoDigestPin_CapturesWhateverTheImageResolvesTo_TodaysBehaviour()
    {
        var imageInspects = 0;
        var docker = new RoutedProcessRunner(args =>
        {
            if (args[0] == "image")
                imageInspects++;
            return args[0] == "inspect"
                ? new ProcessCommandResult(0, "sha256:whatever\t0\t0\t0\tnone\ttrue\tALL\t\n", "")
                : new ProcessCommandResult(0, "", "");
        });
        var runner = new DockerSandboxedSessionRunner(docker, new FrozenClock(DateTimeOffset.UnixEpoch));

        var session = await runner.StartAsync(Spec());
        var attestation = await session.AttestAsync();

        runner.ExpectedImageDigest.Should().BeNull();
        imageInspects.Should().Be(0, "capture-only mode never probes the image ahead of the start");
        attestation.ImageDigest.Should().Be("sha256:whatever");
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
            ? new ProcessCommandResult(0, "ashlar-session-old\t1\n", "")
            : new ProcessCommandResult(0, "", ""));
        var reaper = new DockerSandboxSessionReaper(
            docker, new FrozenClock(DateTimeOffset.FromUnixTimeSeconds(100)), provenance: sink);

        await reaper.SweepAsync();

        sink.Events.Should().ContainSingle().Which.Should().Match<SessionProvenanceEvent>(e =>
            e.Outcome == SessionProvenanceOutcomes.Reaped && e.SessionId == "ashlar-session-old");
    }

    [Fact]
    public void EnvironmentInputs_CarryDigestSpecHashAndAttestationHash_Deterministically()
    {
        var spec = Spec(new ResourceLimits(Memory: "2g"));
        var attestation = Attestation();

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
    public void EnvironmentInputs_HashTheContainmentApplied_AndTheWriteSurfaceDeclared()
    {
        // The certificate must be able to tell "contained" from "asked to be contained":
        // a differently-contained session or a wider write surface is a different hash.
        var spec = Spec(new ResourceLimits(Memory: "2g"));
        var contained = Attestation() with
        {
            EffectiveNetworkMode = "none",
            EffectiveReadOnlyRootFilesystem = true,
            EffectiveDroppedCapabilities = new[] { "ALL" },
            EffectiveSecurityOptions = new[] { "no-new-privileges" },
        };
        var looser = contained with { EffectiveReadOnlyRootFilesystem = false };
        var widerSpec = spec with { ScratchPaths = new[] { "/" } };

        var baseline = SessionEnvironmentInputs.From(spec, contained);
        var looserInputs = SessionEnvironmentInputs.From(spec, looser);
        var widerInputs = SessionEnvironmentInputs.From(widerSpec, contained);

        looserInputs[2].Hash.Should().NotBe(baseline[2].Hash, "the attestation hash covers containment");
        widerInputs[1].Hash.Should().NotBe(baseline[1].Hash, "the sandbox-spec hash covers the write surface");
        widerInputs[2].Hash.Should().Be(baseline[2].Hash, "the attestation itself did not change");
    }

    [Fact]
    public void EnvironmentInputs_RefuseAnUnresolvedImageIdentity()
    {
        var act = () => SessionEnvironmentInputs.From(Spec(), Attestation(digest: null));

        act.Should().Throw<InvalidOperationException>().WithMessage("*image digest*");
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
