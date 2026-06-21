using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Adaptation.Generation;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

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
        Environment.SetEnvironmentVariable("NEXO_CERT_NUGET_CONFIG", null);
        var store = new InMemoryCertificationRecordStore();
        var signer = new CertificationRecordSigner();
        var registry = new CertifiedBrickRegistry(store, signer);
        var gate = new CertificationGate(signer);
        var admission = new CertifiedBrickAdmission(gate, registry);
        var generator = new NewBrickGenerator(model);
        return new GenerateAndCertifyService(generator, admission);
    }

    [Fact(Skip = "HUMAN-AUTHORED WITNESS: populate DamageResolverDogfoodWitness.Spec before enabling")]
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

    [Fact(Skip = "HUMAN-AUTHORED WITNESS: populate DamageResolverDogfoodWitness.Spec before enabling")]
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

    private static WitnessSpec RequireHumanWitness() =>
        DamageResolverDogfoodWitness.Spec
        ?? throw new InvalidOperationException(
            "HUMAN-AUTHORED WITNESS missing — populate DamageResolverDogfoodWitness.Spec");
}
