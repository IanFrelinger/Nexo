using FluentAssertions;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Generation;
using Ashlar.Core.Application.Orchestration;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Bricks.Ports;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Adversarial validation campaign, analyzer half (extension spec §12): scripted
/// adversaries attack the analyzer fence and the manifest rules through the REAL
/// certification chain. Acceptance criterion: zero false certificates — every attack ends
/// in an unadmitted, unsigned FAIL record (or is provably covered by another layer),
/// never a certificate.
/// </summary>
[Trait("Category", "Certification")]
public sealed class AnalyzerGateAdversarialCampaignTests
{
    private static readonly WitnessSpec StrongWitness = new(
        "mutation-probe-brick",
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["logText"] = "2024-01-01 ERROR First failure: connection reset\n2024-01-01 ERROR Second failure: timeout"
                },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "First failure: connection reset"
                })
        ]);

    private static CertificationGate CreateGate() => new(new CertificationRecordSigner());

    private static CertificationRequest RequestFor(string sourceCode, BrickConstraintManifest? manifest = null) => new()
    {
        Brick = new MutationProbeBrick(),
        Witness = StrongWitness,
        SourceCode = sourceCode,
        ProjectPath = CreateCleanProjectFile(),
        CompilationReferences =
        [
            typeof(DomainBrick).Assembly.Location,
            typeof(BrickInput).Assembly.Location,
            typeof(MutationProbeBrick).Assembly.Location
        ],
        BrickTypeName = typeof(MutationProbeBrick).FullName,
        ConstraintManifest = manifest
    };

    /// <summary>The campaign's acceptance criterion, asserted attack by attack.</summary>
    private static void AssertNoFalseCertificate(CertificationDecision decision)
    {
        decision.Admitted.Should().BeFalse("no attack may ever produce an admitted candidate");
        decision.Record.Signed.Should().BeFalse("FAIL records are never signed");
        decision.Record.Status.Should().Be("FAIL");
    }

    [Fact]
    public async Task Attack_alias_dodge_of_a_forbidden_api_never_certifies()
    {
        // Adversary: hide DateTime.Now behind an alias so no forbidden token appears in the
        // text, betting the gate matches strings. It matches resolved symbols (A2.2).
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = ["DateTime.Now"] };
        var attack = MutationProbeBrickSource.Code
            .Replace(
                "using Ashlar.Core.Domain.Execution;",
                "using Ashlar.Core.Domain.Execution;\nusing WallClock = System.DateTime;")
            .Replace(
                "var errorCount = errorLines.Count;",
                "var errorCount = errorLines.Count;\n        if (WallClock.Now.Ticks < 0) errorLines.Clear();");

        var decision = await CreateGate().CertifyAsync(RequestFor(attack, manifest));

        AssertNoFalseCertificate(decision);
        decision.FailureCheck.Should().Be("analyzer");
        decision.Record.Reason.Should().Contain("ASHLAR0011",
            "the manifest rule must see through the alias to the resolved symbol")
            .And.Contain(manifest.ForbiddenApiInstruction("DateTime.Now"));
    }

    [Fact]
    public async Task Attack_hiding_the_token_in_a_string_is_covered_by_the_other_layer()
    {
        // Adversary: embed the forbidden token only inside a string literal. The symbol
        // rules honestly do NOT flag it (nothing nondeterministic executes) — the campaign
        // proves this is a covered handoff, not a gap: the generation-side textual
        // validator rejects the same artifact before it ever reaches certification.
        var manifest = new BrickConstraintManifest { ForbiddenApiTokens = ["DateTime.Now"] };
        var attack = MutationProbeBrickSource.Code.Replace(
            "Summary = $\"Found {errorCount} ERROR line(s); first: {firstErrorMessage}\"",
            "Summary = $\"Found {errorCount} ERROR line(s); first: {firstErrorMessage} DateTime.Now\"");

        var gateOutcome = await new AnalyzerFenceGate().EvaluateAsync(
            attack,
            [typeof(DomainBrick).Assembly.Location],
            manifest);
        gateOutcome.Passed.Should().BeTrue(
            "symbol-resolved rules must not flag inert text — honesty discipline");

        var (validatorOk, validatorReason) = await new BrickConstraintManifestValidator(manifest)
            .ValidateAsync(new GeneratedArtifact { Content = attack }, CancellationToken.None);
        validatorOk.Should().BeFalse("the textual layer catches what the semantic layer honestly cannot");
        validatorReason.Should().Contain(manifest.ForbiddenApiInstruction("DateTime.Now"));
    }

    [Fact]
    public async Task Attack_first_line_violation_cannot_hide_behind_the_wrapper_exemption()
    {
        // Adversary: put the manifest violation on the candidate's very first line, hoping
        // it maps into the wrapper-owned band (candidate line 0) and gets dropped with the
        // preamble. Candidate coordinates always start at 1; only wrapper text maps to 0.
        var manifest = new BrickConstraintManifest { AllowedUsings = ["Ashlar.Core.Domain.Bricks"] };
        var attack = """
            using System.Text;
            public static class Sneak
            {
                public static int Measure(string s) => new StringBuilder(s).Length;
            }
            """;

        var outcome = await new AnalyzerFenceGate().EvaluateAsync(
            attack,
            [typeof(DomainBrick).Assembly.Location],
            manifest);

        outcome.Passed.Should().BeFalse();
        var finding = outcome.Findings.First(
            f => f.Id == Ashlar.Analyzers.BrickConstraintManifestAnalyzer.DisallowedUsingId);
        finding.Line.Should().Be(1, "the candidate's own first line must map to 1, never into the exempt band");
    }

    [Fact]
    public async Task Attack_relaxing_the_severity_floor_env_var_still_denies_certification()
    {
        // Adversary: set the floor env var to "error" hoping warning-severity findings
        // stop counting. The floor is configurable only upward in strictness (A1.3).
        var manifest = new BrickConstraintManifest { ForbiddenNamespaces = ["System.Collections"] };
        Environment.SetEnvironmentVariable(AnalyzerFenceGate.SeverityFloorEnvVar, "error");
        try
        {
            var decision = await CreateGate().CertifyAsync(RequestFor(MutationProbeBrickSource.Code, manifest));

            AssertNoFalseCertificate(decision);
            decision.FailureCheck.Should().Be("analyzer");
        }
        finally
        {
            Environment.SetEnvironmentVariable(AnalyzerFenceGate.SeverityFloorEnvVar, null);
        }
    }

    [Fact]
    public async Task Attack_non_compiling_candidate_cannot_ride_vacuous_mutation_kills()
    {
        // Adversary: submit source that does not compile, betting on the historical hole
        // where non-compiling mutants counted as killed and analyzer silence read as a
        // pass. The fence fails it closed before any downstream gate runs (A1.4).
        var decision = await CreateGate().CertifyAsync(RequestFor("public sealed class Broken {"));

        AssertNoFalseCertificate(decision);
        decision.FailureCheck.Should().Be("analyzer");
        decision.Record.Reason.Should().Contain("does not compile");
        decision.Record.GatesPassed.Should().BeEmpty();
    }

    [Fact]
    public async Task Attack_substituting_a_weaker_manifest_cannot_widen_the_gate()
    {
        // Adversary: claim the proposer was shown a permissive manifest and hope the gate
        // uses that one. There is no second channel: the ONLY manifest the gate sees is
        // the instance on the request, so whoever builds the request decides — and the
        // decision is recorded via the restated instruction in the failure reason.
        var strict = new BrickConstraintManifest { ForbiddenNamespaces = ["System.Collections"] };
        _ = new BrickConstraintManifest(); // the adversary's alleged "weak" manifest, unused by construction

        var decision = await CreateGate().CertifyAsync(RequestFor(MutationProbeBrickSource.Code, strict));

        AssertNoFalseCertificate(decision);
        decision.Record.Reason.Should().Contain(strict.ForbiddenNamespaceInstruction("System.Collections"),
            "the enforced rules restate the request manifest's instructions verbatim — the only source there is");
    }

    private static string CreateCleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ashlar-cert-adversarial-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Ashlar.Brick.Contracts" Version="0.1.0" />
    <PackageReference Include="Ashlar.Authoring" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }
}
