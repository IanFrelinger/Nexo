using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Application.Certification.Ports;
using Ashlar.Infrastructure.Adaptation;
using Ashlar.Infrastructure.Adaptation.Generation;
using Ashlar.Infrastructure.Certification;
using Ashlar.Infrastructure.Certification.Composition;
using Ashlar.Tests.Infrastructure.Certification.Dogfood;
using Ashlar.Tests.Infrastructure.Certification.Fixtures;

namespace Ashlar.Tests.Infrastructure.Certification.Dogfood;

/// <summary>
/// Shared harness: certifies damage-resolver + health-applier constituents and wires composition gate.
/// </summary>
public static class CompositionDogfoodHarness
{
    public static readonly IntentSpec DamageResolverIntent = new(
        CursorGeneratorModel.DamageResolverIntentId,
        "Given baseDamage, critMultiplierPercent, armor, and isCrit, compute final damage as crit-adjusted raw minus armor (floored at zero).",
        BrickId: CursorGeneratorModel.DamageResolverIntentId,
        Name: "Damage Resolver");

    public static readonly IntentSpec HealthApplierIntent = new(
        CursorGeneratorModel.HealthApplierIntentId,
        "Given currentHealth and finalDamage, compute newHealth as max(0, currentHealth - finalDamage).",
        BrickId: CursorGeneratorModel.HealthApplierIntentId,
        Name: "Health Applier");

    public static CompositionWitnessSpec RequireHumanWitness()
    {
        if (CompositionDogfoodWitness.Spec is null)
            throw new InvalidOperationException("HUMAN-AUTHORED WITNESS: populate CompositionDogfoodWitness.Spec");

        return CompositionDogfoodWitness.Spec;
    }

    public static async Task<CertifiedCompositionTestContext> CreateAdmittedContextAsync()
    {
        Environment.SetEnvironmentVariable("ASHLAR_CERT_NUGET_CONFIG", null);

        var brickStore = new InMemoryCertificationRecordStore();
        var (privateKey, _) = CreateEd25519Key();
        var brickSigner = new CertificationRecordSigner(ed25519PrivateKeyBase64: privateKey);
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

    public static CompositionProposerInput BuildProposerInput(CertifiedCompositionTestContext ctx)
    {
        var target = Ashlar.Core.Application.Certification.CompositionProposerInputBuilder.TargetFromSpec(
            CompositionDogfoodFixtures.HonestSpec());
        return Ashlar.Core.Application.Certification.CompositionProposerInputBuilder.FromTargetAndRegistry(
            target,
            ctx.BrickRegistry,
            ctx.BrickStore);
    }

    public static CompositionProposerInput BuildProposerInputForIndependenceCheck()
    {
        var target = Ashlar.Core.Application.Certification.CompositionProposerInputBuilder.TargetFromSpec(
            CompositionDogfoodFixtures.HonestSpec());

        return new CompositionProposerInput(
            target,
            [
                new CertifiedBrickCatalogEntry(
                    CursorGeneratorModel.DamageResolverIntentId,
                    [
                        new WitnessIoField("baseDamage", "int"),
                        new WitnessIoField("critMultiplierPercent", "int"),
                        new WitnessIoField("armor", "int"),
                        new WitnessIoField("isCrit", "bool")
                    ],
                    [new WitnessIoField("finalDamage", "int")],
                    "damage-resolver@2026-06-21T00:00:00.0000000Z"),
                new CertifiedBrickCatalogEntry(
                    CursorGeneratorModel.HealthApplierIntentId,
                    [
                        new WitnessIoField("currentHealth", "int"),
                        new WitnessIoField("finalDamage", "int")
                    ],
                    [new WitnessIoField("newHealth", "int")],
                    "health-applier@2026-06-21T00:00:00.0000000Z")
            ]);
    }

    /// <summary>
    /// Atom-level witness for health-applier constituent certification only — not the composition witness.
    /// </summary>
    public static readonly WitnessSpec HealthApplierConstituentWitness = new(
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

    private static (string PrivateKeyBase64, string PublicKeyBase64) CreateEd25519Key()
    {
        using var key = NSec.Cryptography.Key.Create(
            NSec.Cryptography.SignatureAlgorithm.Ed25519,
            new NSec.Cryptography.KeyCreationParameters { ExportPolicy = NSec.Cryptography.KeyExportPolicies.AllowPlaintextExport });
        return (
            Convert.ToBase64String(key.Export(NSec.Cryptography.KeyBlobFormat.RawPrivateKey)),
            Convert.ToBase64String(key.PublicKey.Export(NSec.Cryptography.KeyBlobFormat.RawPublicKey)));
    }
}
