using FluentAssertions;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;
using Nexo.Infrastructure.Adaptation;
using Nexo.Infrastructure.Adaptation.Generation;
using Nexo.Infrastructure.Certification;
using Nexo.Infrastructure.Certification.Composition;
using Nexo.Tests.Infrastructure.Certification.Dogfood;
using Nexo.Tests.Infrastructure.Certification.Fixtures;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CompositionDogfoodTests
{
    private static readonly IntentSpec DamageResolverIntent = new(
        CursorGeneratorModel.DamageResolverIntentId,
        "Given baseDamage, critMultiplierPercent, armor, and isCrit, compute final damage as crit-adjusted raw minus armor (floored at zero).",
        BrickId: CursorGeneratorModel.DamageResolverIntentId,
        Name: "Damage Resolver");

    private static readonly IntentSpec HealthApplierIntent = new(
        CursorGeneratorModel.HealthApplierIntentId,
        "Given currentHealth and finalDamage, compute newHealth as max(0, currentHealth - finalDamage).",
        BrickId: CursorGeneratorModel.HealthApplierIntentId,
        Name: "Health Applier");

    [Fact]
    public async Task HonestComposition_StrongWitness_Admits_WithZeroEscapeRate()
    {
        var witness = RequireHumanWitness();
        var ctx = await CreateAdmittedContextAsync();

        var decision = await ctx.CompositionAdmission.CertifyAndAdmitAsync(new CompositionCertificationRequest
        {
            Spec = CompositionDogfoodFixtures.HonestSpec(),
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeTrue(
            $"expected ADMIT, got {decision.FailureCheck}: {decision.Record.Reason}");
        decision.Record.CompositionEscapeRate.Should().Be(0);
        decision.Record.Signed.Should().BeTrue();
        ctx.CompositionSigner.Verify(decision.Record).Should().BeTrue();
    }

    [Fact]
    public async Task BrokenComposition_StrongWitness_Rejects()
    {
        var witness = RequireHumanWitness();
        var ctx = await CreateAdmittedContextAsync();

        var decision = await ctx.CompositionGate.CertifyAsync(new CompositionCertificationRequest
        {
            Spec = CompositionDogfoodFixtures.BrokenWiringSpec(),
            Witness = witness,
            WiringMetadata = "composition-wiring-v0"
        });

        decision.Admitted.Should().BeFalse("broken composition wiring must not admit");
        decision.FailureCheck.Should().BeOneOf("seam", "correctness", "mutation");
    }

    private static CompositionWitnessSpec RequireHumanWitness()
    {
        if (CompositionDogfoodWitness.Spec is null)
            throw new InvalidOperationException("HUMAN-AUTHORED WITNESS: populate CompositionDogfoodWitness.Spec");

        return CompositionDogfoodWitness.Spec;
    }

    private static async Task<CertifiedCompositionTestContext> CreateAdmittedContextAsync()
    {
        Environment.SetEnvironmentVariable("NEXO_CERT_NUGET_CONFIG", null);

        var brickStore = new InMemoryCertificationRecordStore();
        var brickSigner = new CertificationRecordSigner();
        var brickRegistry = new CertifiedBrickRegistry(brickStore, brickSigner);
        var brickGate = new CertificationGate(brickSigner);
        var brickAdmission = new CertifiedBrickAdmission(brickGate, brickRegistry);
        var generator = new NewBrickGenerator(new CursorGeneratorModel { Variant = "honest" });
        var generateAndCertify = new GenerateAndCertifyService(generator, brickAdmission);

        var damageResult = await generateAndCertify.GenerateCertifyAndAdmitAsync(
            DamageResolverIntent,
            DamageResolverDogfoodWitness.Spec).ConfigureAwait(false);

        if (!damageResult.Decision.Admitted)
        {
            throw new InvalidOperationException(
                $"Failed to admit damage-resolver: {damageResult.Decision.FailureCheck} {damageResult.Decision.Record.Reason}");
        }

        var healthResult = await generateAndCertify.GenerateCertifyAndAdmitAsync(
            HealthApplierIntent,
            HealthApplierConstituentWitness).ConfigureAwait(false);

        if (!healthResult.Decision.Admitted)
        {
            throw new InvalidOperationException(
                $"Failed to admit health-applier: {healthResult.Decision.FailureCheck} {healthResult.Decision.Record.Reason}");
        }

        var compositionStore = new InMemoryCompositionCertificationRecordStore();
        var compositionSigner = new CompositionCertificationRecordSigner(brickSigner);
        var compositionRegistry = new CertifiedCompositionRegistry(compositionStore, compositionSigner);
        var compositionGate = new CompositionCertificationGate(
            brickStore,
            brickSigner,
            brickRegistry,
            compositionSigner);
        var compositionAdmission = new CertifiedCompositionAdmission(compositionGate, compositionRegistry);

        return new CertifiedCompositionTestContext(
            brickStore,
            brickSigner,
            brickRegistry,
            brickAdmission,
            compositionStore,
            compositionSigner,
            compositionRegistry,
            compositionGate,
            compositionAdmission);
    }

    /// <summary>
    /// Atom-level witness for health-applier constituent certification only — not the composition witness.
    /// </summary>
    private static readonly WitnessSpec HealthApplierConstituentWitness = new(
        CursorGeneratorModel.HealthApplierIntentId,
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["currentHealth"] = 100,
                    ["finalDamage"] = 30
                },
                new Dictionary<string, object> { ["newHealth"] = 70 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["currentHealth"] = 50,
                    ["finalDamage"] = 60
                },
                new Dictionary<string, object> { ["newHealth"] = 0 }),

            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["currentHealth"] = 0,
                    ["finalDamage"] = 10
                },
                new Dictionary<string, object> { ["newHealth"] = 0 })
        ]);
}
