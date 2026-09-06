using FluentAssertions;
using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Autonomy;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.HotSwap;
using NSec.Cryptography;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Cadence, pause, single-op controls, and the digest (autonomy spec §6–§7): the pause
/// halts autonomous swaps immediately while human-driven swaps proceed, the cadence floor
/// spaces autonomous swaps, an in-flight watch window blocks the same lineage, every
/// operator control is a single offline operation, and the digest keeps "did" and
/// "holding" visibly distinct.
/// </summary>
[Trait("Category", "Certification")]
[Collection("hot-swap-host")]
public sealed class AutonomyControlsTests
{
    private const string HmacKey = "autonomy-controls-test-hmac";
    private const string ProbeBrickId = "hot-swap-probe";

    private const string ProbeTemplate = """
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Tests.HotSwapProbe;

public sealed class HotSwapProbeBrick : DomainBrick
{
    public HotSwapProbeBrick()
    {
        Id = "hot-swap-probe";
        Name = "Hot Swap Probe";
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        Ashlar.Core.Domain.Execution.IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var output = new BrickOutput { Summary = "{MARKER}" };
        output.Set("marker", "{MARKER}");
        return Task.FromResult(output);
    }
}
""";

    [Fact]
    public async Task Pause_HaltsAutonomousSwapsImmediately_WhileHumanSwapsProceed()
    {
        var pause = new LoopPauseControl();
        using var host = CreateHost(pause: pause);
        pause.Pause("operator investigating");

        var autonomous = await host.SwapAsync(new[] { AutonomousRequest(Probe("v1"), "lineage-a") });
        autonomous.Swapped.Should().BeFalse();
        autonomous.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("loop-paused");

        // Pause bounds the LOOP, not the operator (R7.2 controls stay usable).
        (await host.SwapAsync(new[] { HumanRequest(Probe("v1h")) })).Swapped.Should().BeTrue();

        // Resume is lossless: the same request now swaps with no reconstruction.
        pause.Resume();
        (await host.SwapAsync(new[] { AutonomousRequest(Probe("v2"), "lineage-a") })).Swapped.Should().BeTrue();
    }

    [Fact]
    public async Task CadenceFloor_SpacesAutonomousSwaps_AndTimePassingUnblocks()
    {
        var clock = new MutableClock(DateTimeOffset.UnixEpoch);
        using var host = CreateHost(cadenceFloor: TimeSpan.FromMinutes(10), clock: clock);

        (await host.SwapAsync(new[] { AutonomousRequest(Probe("v1"), "lineage-a") })).Swapped.Should().BeTrue();

        var tooFast = await host.SwapAsync(new[] { AutonomousRequest(Probe("v2"), "lineage-b") });
        tooFast.Swapped.Should().BeFalse();
        tooFast.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("cadence-floor");

        clock.Advance(TimeSpan.FromMinutes(11));
        (await host.SwapAsync(new[] { AutonomousRequest(Probe("v2"), "lineage-b") })).Swapped.Should().BeTrue();
    }

    [Fact]
    public async Task InFlightWatchWindow_BlocksTheSameLineage_UntilItClears()
    {
        using var host = CreateHost(watch: new WatchThresholds { MinInvocations = 2 });

        (await host.SwapAsync(new[] { AutonomousRequest(Probe("v1"), "lineage-a") })).Swapped.Should().BeTrue();

        // Zero invocations observed: the window is in flight for lineage-a...
        var blocked = await host.SwapAsync(new[] { AutonomousRequest(Probe("v2"), "lineage-a") });
        blocked.Refusals.Should().ContainSingle().Which.FailureCode.Should().Be("watch-window-in-flight");

        // ...but a DIFFERENT lineage is not blocked by it (per-lineage semantics, R6.1).
        (await host.SwapAsync(new[] { AutonomousRequest(Probe("v2"), "lineage-b") })).Swapped.Should().BeTrue();
    }

    [Fact]
    public void SingleOpControls_DemoteAndPause_RequireNoProposalSession()
    {
        var authority = new InMemoryLineageAuthority();
        authority.IsDemoted("lineage-x").Should().BeFalse();
        authority.Demote("lineage-x", "operator decision");
        authority.IsDemoted("lineage-x").Should().BeTrue("R7.2: demotion is one operation");

        var pause = new LoopPauseControl();
        pause.Pause("one call");
        pause.IsPaused.Should().BeTrue();
        pause.PausedReason.Should().Be("one call");
    }

    [Fact]
    public void Digest_KeepsDidAndHolding_VisiblyDistinct()
    {
        var events = new[]
        {
            new BrickSwapProvenanceEvent
            {
                Generation = 1,
                Outcome = BrickSwapProvenanceOutcomes.SwapCommitted,
                Timestamp = DateTimeOffset.UnixEpoch,
            },
            new BrickSwapProvenanceEvent
            {
                Generation = 2,
                Outcome = BrickSwapProvenanceOutcomes.WatchBreachQuarantined,
                Timestamp = DateTimeOffset.UnixEpoch.AddMinutes(5),
                Reason = "Watch breach: error rate 1.00 exceeds baseline 0.00",
            },
        };
        var held = new[]
        {
            new AutonomyDigest.HeldArtifact("policy-brick", "sha256:held", "Tier 1: awaiting human admission"),
        };

        var digest = AutonomyDigest.Render(events, held);

        digest.Should().Contain("## The loop did")
            .And.Contain("swap-committed")
            .And.Contain("watch-breach-quarantined")
            .And.Contain("## The loop proposes and is holding")
            .And.Contain("**HELD** policy-brick")
            .And.Contain("awaiting human admission",
                "held artifacts are presented with evidence, never auto-expired (R7.3)");
    }

    // --- helpers -------------------------------------------------------------------------

    private static string Probe(string marker) => ProbeTemplate.Replace("{MARKER}", marker);

    private static CertifiedBrickHotSwapHost CreateHost(
        LoopPauseControl? pause = null,
        TimeSpan? cadenceFloor = null,
        TimeProvider? clock = null,
        WatchThresholds? watch = null) =>
        new(null, logger: null, hmacKey: HmacKey, drainTimeout: TimeSpan.FromSeconds(10),
            revocations: new InMemoryCertificateRevocationList(), retentionWindow: 2,
            watchThresholds: watch, lineageAuthority: new InMemoryLineageAuthority(),
            pauseControl: pause, cadenceFloor: cadenceFloor, clock: clock);

    private static CertifiedBrickLoadRequest AutonomousRequest(string source, string lineageKey) => new()
    {
        BrickId = ProbeBrickId,
        SourceCode = source,
        Record = CertifyRecord(ProbeBrickId, source),
        Autonomous = new AutonomousAdmission
        {
            Tier = ObjectiveTier.Tier0Autonomous,
            Lineage = GenerationLineage.Child(GenerationLineage.HumanAuthored, "sig-parent"),
            LineageKey = lineageKey,
        }
    };

    private static CertifiedBrickLoadRequest HumanRequest(string source) => new()
    {
        BrickId = ProbeBrickId,
        SourceCode = source,
        Record = CertifyRecord(ProbeBrickId, source),
    };

    private static CertificationRecordData CertifyRecord(string brickId, string source)
    {
        var (privateKey, publicKey) = CreateEd25519Key();
        
        var record = new CertificationRecordData
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = brickId,
            ContentHash = BrickContentHasher.ComputeSha256(source),
            Gate = "autonomy-controls-test-harness",
            SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion,
            Inputs =
            [
                new CertificationInput
                {
                    Kind = CertificationInputKinds.GateEmittedArtifact,
                    Id = brickId,
                    Hash = BrickContentHasher.ComputeSha256(source)
                },
                CertifierIdentity.ToInput()
            ],
            Ed25519PublicKey = publicKey
        };

        return record with
        {
            Signature = CertificationRecordSigning.Sign(record, HmacKey),
            Ed25519Signature = CertificationRecordEd25519.Sign(record, Convert.FromBase64String(privateKey))
        };
    }

    private static (string PrivateKeyBase64, string PublicKeyBase64) CreateEd25519Key()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return (
            Convert.ToBase64String(key.Export(KeyBlobFormat.RawPrivateKey)),
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)));
    }

    private sealed class MutableClock : TimeProvider
    {
        private DateTimeOffset _now;
        public MutableClock(DateTimeOffset start) => _now = start;
        public void Advance(TimeSpan by) => _now += by;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
