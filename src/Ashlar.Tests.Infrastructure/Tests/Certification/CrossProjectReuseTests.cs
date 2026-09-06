using FluentAssertions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Adaptation.Generation;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Dogfood;
using Ashlar.Tests.Infrastructure.Certification.Reuse;
using NSec.Cryptography;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

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

        var trust = ProjectBTrustConsumer.VerifyArtifact(
            artifact.SourceCode,
            record,
            artifact.AssemblyBytes);
        trust.Trusted.Should().BeTrue($"expected TRUSTED, got {trust.FailureCode}: {trust.Reason}");
        record.ContentHash.Should().NotBeNullOrWhiteSpace();
        record.Inputs.Should().Contain(i => i.Kind == Ashlar.Certification.Contracts.CertificationInputKinds.GateEmittedArtifact);

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

        var trust = ProjectBTrustConsumer.VerifyArtifact(tamperedSource, record, artifact.AssemblyBytes);
        trust.Trusted.Should().BeFalse("tampered brick must not be trusted");
        trust.FailureCode.Should().Be("content-hash-mismatch");
    }

    [Fact]
    public async Task SwappedPe_ProjectB_RejectsArtifactHashMismatch()
    {
        var artifact = await ProjectACertifiesDamageResolverAsync();
        var record = ProjectBTrustConsumer.FromInternalRecord(artifact.Record);
        var swapped = artifact.AssemblyBytes.ToArray();
        swapped[Math.Min(0x80, swapped.Length - 1)] ^= 0xFF;

        var trust = ProjectBTrustConsumer.VerifyArtifact(artifact.SourceCode, record, swapped);
        trust.Trusted.Should().BeFalse();
        trust.FailureCode.Should().Be("artifact-hash-mismatch");
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
        Environment.SetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG", null);
        var store = new InMemoryCertificationRecordStore();
        var (privateKey, _) = CreateEd25519Key();
        var signer = new CertificationRecordSigner(ed25519PrivateKeyBase64: privateKey);
        var registry = new CertifiedBrickRegistry(store, signer);
        var gate = new CertificationGate(signer);
        var admission = new CertifiedBrickAdmission(gate, registry);
        var generator = new NewBrickGenerator(new CursorGeneratorModel { Variant = "honest" });
        var service = new GenerateAndCertifyService(generator, admission);

        var witness = DamageResolverDogfoodWitness.Spec;
        var signature = Ashlar.Core.Application.Adaptation.WitnessSignatureBuilder.FromWitness(witness);
        var manifest = await generator.GenerateFromIntentAsync(DamageResolverIntent, signature).ConfigureAwait(false);
        var built = await GeneratedBrickBuilder.BuildAsync(manifest).ConfigureAwait(false);

        var decision = await admission.CertifyAndAdmitAsync(new CertificationRequest
        {
            Brick = built.Brick,
            Witness = witness,
            SourceCode = built.SourceCode,
            ProjectPath = built.ProjectPath,
            CompilationReferences = built.CompilationReferences,
            BrickTypeName = built.BrickTypeName,
            EmittedArtifact = built.EmittedArtifact
        }).ConfigureAwait(false);

        decision.Admitted.Should().BeTrue("project A must certify damage-resolver");
        decision.Record.ContentHash.Should().NotBeNullOrWhiteSpace();

        return new CertifiedArtifact(
            built.SourceCode,
            decision.Record,
            built.BrickTypeName,
            built.EmittedArtifact.AssemblyBytes);
    }

    private sealed record CertifiedArtifact(
        string SourceCode,
        CertificationRecord Record,
        string? BrickTypeName,
        byte[] AssemblyBytes);

    private static (string PrivateKeyBase64, string PublicKeyBase64) CreateEd25519Key()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return (
            Convert.ToBase64String(key.Export(KeyBlobFormat.RawPrivateKey)),
            Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey)));
    }
}
