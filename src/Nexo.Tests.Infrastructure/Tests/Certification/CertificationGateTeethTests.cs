using FluentAssertions;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.Sdk.Extensions;
using Nexo.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CertificationGateTeethTests
{
    private static readonly string ProbeLog =
        "2024-01-01 INFO Started\n2024-01-01 ERROR First failure: connection reset\n2024-01-01 WARN Retrying\n2024-01-01 ERROR Second failure: timeout";

    private static readonly WitnessSpec StrongWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = ProbeLog },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "First failure: connection reset"
                })
        ]);

    private static readonly WitnessSpec WeakWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = ProbeLog },
                new Dictionary<string, object> { ["errorCount"] = 2 })
        ]);

    private static CertificationGate CreateGate() => new(new CertificationRecordSigner());

    private static List<string> CompilationReferences()
    {
        return
        [
            typeof(Brick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
            typeof(MutationProbeBrick).Assembly.Location
        ];
    }

    [Fact]
    public async Task GoodBrick_StrongWitness_Admits_WithZeroEscapeRate()
    {
        var gate = CreateGate();
        var brick = new MutationProbeBrick();
        var request = new CertificationRequest
        {
            Brick = brick,
            Witness = StrongWitness,
            SourceCode = MutationProbeBrickSource.Code,
            ProjectPath = CreateCleanProjectFile(),
            CompilationReferences = CompilationReferences(),
            BrickTypeName = typeof(MutationProbeBrick).FullName
        };

        var decision = await gate.CertifyAsync(request);

        decision.Admitted.Should().BeTrue();
        decision.Record.EscapeRate.Should().Be(0);
        decision.Record.Signed.Should().BeTrue();
        decision.Record.Signature.Should().NotBeNullOrWhiteSpace();
        decision.Record.ContentHash.Should().NotBeNullOrWhiteSpace();
        new CertificationRecordSigner().Verify(decision.Record).Should().BeTrue();
    }

    [Fact]
    public async Task BadWitnessBrick_Rejects_OnCorrectness()
    {
        var gate = CreateGate();
        var request = new CertificationRequest
        {
            Brick = new BadWitnessBrick(),
            Witness = StrongWitness with { BrickId = "bad-witness-brick" },
            SourceCode = "namespace X; class Y {}",
            ProjectPath = CreateCleanProjectFile(),
            CompilationReferences = CompilationReferences(),
            BrickTypeName = typeof(BadWitnessBrick).FullName
        };

        var decision = await gate.CertifyAsync(request);

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("correctness");
    }

    [Fact]
    public async Task WeakWitness_AllowsMutantEscapes_RejectsWithTeeth()
    {
        var gate = CreateGate();
        var brick = new MutationProbeBrick();
        var request = new CertificationRequest
        {
            Brick = brick,
            Witness = WeakWitness,
            SourceCode = MutationProbeBrickSource.Code,
            ProjectPath = CreateCleanProjectFile(),
            CompilationReferences = CompilationReferences(),
            BrickTypeName = typeof(MutationProbeBrick).FullName
        };

        var decision = await gate.CertifyAsync(request);

        decision.Admitted.Should().BeFalse("weak witness must not admit when mutants survive");
        decision.FailureCheck.Should().Be("mutation");
        decision.Record.EscapeRate.Should().BeGreaterThan(0);
        decision.Record.SurvivingMutantIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task NondeterministicBrick_Rejects_OnDeterminism()
    {
        var gate = CreateGate();
        var request = new CertificationRequest
        {
            Brick = new NondeterministicBrick(),
            Witness = new WitnessSpec(
                "nondeterministic-brick",
                [
                    new WitnessCase(
                        new Dictionary<string, object> { ["logText"] = ProbeLog },
                        new Dictionary<string, object>
                        {
                            ["errorCount"] = 2,
                            ["firstErrorMessage"] = "First failure: connection reset"
                        })
                ]),
            SourceCode = NondeterministicBrickSource.Code,
            ProjectPath = CreateCleanProjectFile(),
            CompilationReferences = CompilationReferences(),
            BrickTypeName = typeof(NondeterministicBrick).FullName
        };

        var decision = await gate.CertifyAsync(request);

        decision.Admitted.Should().BeFalse();
        decision.FailureCheck.Should().Be("determinism");
    }

    [Fact]
    public void UngatedBrick_IsRejectedByRegistryAdmissionPath()
    {
        var store = new InMemoryCertificationRecordStore();
        var signer = new CertificationRecordSigner();
        var registry = new CertifiedBrickRegistry(store, signer);
        var brick = new MutationProbeBrick();

        var admitted = registry.TryAdmit(brick, new CertificationRecord
        {
            Status = "PASS",
            Stage = "S0-S2",
            Admitted = true,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = brick.Id,
            Signature = "not-a-real-signature"
        });

        admitted.Should().BeFalse();
        registry.GetBrick(brick.Id).Should().BeNull();
    }

    [Fact]
    public async Task CertifiedAdmission_OnlyExposesAdmittedBricks()
    {
        var store = new InMemoryCertificationRecordStore();
        var signer = new CertificationRecordSigner();
        var registry = new CertifiedBrickRegistry(store, signer);
        var gate = new CertificationGate(signer);
        var admission = new CertifiedBrickAdmission(gate, registry);

        var request = new CertificationRequest
        {
            Brick = new MutationProbeBrick(),
            Witness = StrongWitness,
            SourceCode = MutationProbeBrickSource.Code,
            ProjectPath = CreateCleanProjectFile(),
            CompilationReferences = CompilationReferences(),
            BrickTypeName = typeof(MutationProbeBrick).FullName
        };

        var decision = await admission.CertifyAndAdmitAsync(request);
        decision.Admitted.Should().BeTrue();
        registry.GetBrick("mutation-probe-brick").Should().NotBeNull();

        var ungated = new BadWitnessBrick();
        registry.TryAdmit(ungated, decision.Record with { BrickId = ungated.Id, Signature = "bogus" }).Should().BeFalse();
        registry.GetBrick(ungated.Id).Should().BeNull();
    }

    private static string CreateCleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-cert-clean-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Nexo.Brick.Contracts" Version="0.1.0" />
    <PackageReference Include="Nexo.Authoring" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }
}
