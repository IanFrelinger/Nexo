using FluentAssertions;
using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Adaptation.Ports;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Adaptation.Generation;
using Ashlar.Infrastructure.Certification;
using Ashlar.Tests.Infrastructure.Certification.Dogfood;
using NSec.Cryptography;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for damage resolver dogfood.</summary>
[Trait("Category", "Certification")]
public sealed class DamageResolverDogfoodTests
{
    private static readonly IntentSpec Intent = new(
        CursorGeneratorModel.DamageResolverIntentId,
        "Given baseDamage, critMultiplierPercent, armor, and isCrit, compute final damage as crit-adjusted raw minus armor (floored at zero).",
        BrickId: CursorGeneratorModel.DamageResolverIntentId,
        Name: "Damage Resolver");

    private static GenerateAndCertifyService CreateService(CursorGeneratorModel model)
    {
        Environment.SetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG", null);
        var store = new InMemoryCertificationRecordStore();
        var (privateKey, _) = CreateEd25519Key();
        var signer = new CertificationRecordSigner(ed25519PrivateKeyBase64: privateKey);
        var registry = new CertifiedBrickRegistry(store, signer);
        var gate = new CertificationGate(signer);
        var admission = new CertifiedBrickAdmission(gate, registry);
        var generator = new NewBrickGenerator(model);
        /// <summary>Generate and certify service.</summary>
        return new GenerateAndCertifyService(generator, admission);
    }

    [Fact]
    public async Task HonestCursorGeneration_Admits_WithZeroEscapeRate()
    {
        var witness = RequireHumanWitness();
        var model = new CursorGeneratorModel { Variant = "honest" };
        var service = CreateService(model);
        var result = await service.GenerateCertifyAndAdmitAsync(Intent, witness);

        result.Decision.Admitted.Should().BeTrue(
            $"expected ADMIT, got {result.Decision.FailureCheck}: {result.Decision.Record.Reason}");
        result.Decision.Record.EscapeRate.Should().Be(0);
        result.Decision.Record.Signed.Should().BeTrue();
        result.Manifest!.GenerationProvenance.Should().StartWith("cursor:honest");
    }

    [Fact]
    public async Task BuggyCursorGeneration_Rejects()
    {
        var witness = RequireHumanWitness();
        var model = new CursorGeneratorModel { Variant = "buggy" };
        var service = CreateService(model);
        var result = await service.GenerateCertifyAndAdmitAsync(Intent, witness);

        result.Decision.Admitted.Should().BeFalse("subtly wrong cursor generation must not admit");
        result.Decision.FailureCheck.Should().BeOneOf("correctness", "mutation");
        result.Manifest!.GenerationProvenance.Should().StartWith("cursor:buggy");
    }

    /// <summary>Require human witness.</summary>
    private static WitnessSpec RequireHumanWitness() => DamageResolverDogfoodWitness.Spec;

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
