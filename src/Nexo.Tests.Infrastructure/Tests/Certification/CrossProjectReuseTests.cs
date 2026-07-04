using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Adaptation.Generation;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Nexo.Tests.Infrastructure.Certification.Reuse;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for cross project reuse.</summary>
[Trait("Category", "Certification")]
public sealed class CrossProjectReuseTests
{
    private static readonly IntentSpec DamageResolverIntent = new(
        CursorGeneratorModel.DamageResolverIntentId,
        "Given baseDamage, critMultiplierPercent, armor, and isCrit, compute final damage.",
        BrickId: CursorGeneratorModel.DamageResolverIntentId,
        Name: "Damage Resolver");

    [Fact]
    public async Task HonestCertifiedBrick_ProjectB_TrustsAndRunsUntouched()
    {
        var projectBcsproj = Path.Combine(
            RepoPaths.FindRepoRoot(),
            "samples", "certified-brick-reuse", "ProjectB", "ProjectB.csproj");
        ProjectBTrustConsumer.AssertProjectBProjectHasNoGateOrGeneratorReferences(projectBcsproj);

        var artifact = await ProjectACertifiesDamageResolverAsync();
        var record = ProjectBTrustConsumer.FromInternalRecord(artifact.Record);

        var trust = ProjectBTrustConsumer.VerifyArtifact(artifact.SourceCode, record);
        trust.Trusted.Should().BeTrue($"expected TRUSTED, got {trust.FailureCode}: {trust.Reason}");
        record.ContentHash.Should().NotBeNullOrWhiteSpace();

        var finalDamage = await ProjectBTrustConsumer.ExecuteDamageResolverSmokeAsync(
            artifact.SourceCode,
            artifact.BrickTypeName!);
        finalDamage.Should().Be(40);
    }

    [Fact]
    public async Task TamperedBrick_ProjectB_RejectsContentHashMismatch()
    {
        var artifact = await ProjectACertifiesDamageResolverAsync();
        var record = ProjectBTrustConsumer.FromInternalRecord(artifact.Record);
        var tamperedSource = artifact.SourceCode.Replace("Math.Max(0, raw - armor)", "Math.Max(0, raw - armor + 1)");

        var trust = ProjectBTrustConsumer.VerifyArtifact(tamperedSource, record);
        trust.Trusted.Should().BeFalse("tampered brick must not be trusted");
        trust.FailureCode.Should().Be("content-hash-mismatch");
    }

    [Fact]
    public async Task ForgedSignature_ProjectB_Rejects()
    {
        var artifact = await ProjectACertifiesDamageResolverAsync();
        var record = ProjectBTrustConsumer.FromInternalRecord(artifact.Record) with
        {
            Signature = Convert.ToBase64String(new byte[32])
        };

        var trust = ProjectBTrustConsumer.VerifyArtifact(artifact.SourceCode, record);
        trust.Trusted.Should().BeFalse("forged signature must not be trusted");
        trust.FailureCode.Should().Be("signature-invalid");
    }

    private static async Task<CertifiedArtifact> ProjectACertifiesDamageResolverAsync()
    {
        Environment.SetEnvironmentVariable("NEXO_CERT_NUGET_CONFIG", null);
        var store = new InMemoryCertificationRecordStore();
        var signer = new CertificationRecordSigner();
        var registry = new CertifiedBrickRegistry(store, signer);
        var gate = new CertificationGate(signer);
        var admission = new CertifiedBrickAdmission(gate, registry);
        var generator = new NewBrickGenerator(new CursorGeneratorModel { Variant = "honest" });
        var service = new GenerateAndCertifyService(generator, admission);

        var witness = DamageResolverDogfoodWitness.Spec;
        var signature = Nexo.Core.Application.Adaptation.WitnessSignatureBuilder.FromWitness(witness);
        var manifest = await generator.GenerateFromIntentAsync(DamageResolverIntent, signature).ConfigureAwait(false);
        var built = await GeneratedBrickBuilder.BuildAsync(manifest).ConfigureAwait(false);

        var decision = await admission.CertifyAndAdmitAsync(new CertificationRequest
        {
            Brick = built.Brick,
            Witness = witness,
            SourceCode = built.SourceCode,
            ProjectPath = built.ProjectPath,
            CompilationReferences = built.CompilationReferences,
            BrickTypeName = built.BrickTypeName
        }).ConfigureAwait(false);

        decision.Admitted.Should().BeTrue("project A must certify damage-resolver");
        decision.Record.ContentHash.Should().NotBeNullOrWhiteSpace();

        /// <summary>Certified artifact.</summary>
        return new CertifiedArtifact(built.SourceCode, decision.Record, built.BrickTypeName);
    }

    /// <summary>Tests for certified artifact.</summary>
    /// <param name="SourceCode">Source code.</param>
    /// <param name="Record">Record.</param>
    /// <param name="BrickTypeName">Brick type name.</param>
    private sealed record CertifiedArtifact(string SourceCode, CertificationRecord Record, string? BrickTypeName);
}
