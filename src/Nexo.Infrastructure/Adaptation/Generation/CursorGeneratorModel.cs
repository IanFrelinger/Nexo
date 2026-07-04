using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// <strong>TEST DOUBLE</strong> — stand-in for <see cref="ProviderGeneratorModel"/> in dogfood certification tests.
/// Returns Cursor-authored source written at sprint time (not a live API or production generation).
/// Witness cases are never seen — only <see cref="WitnessSignature"/> I/O.
/// </summary>
public sealed class CursorGeneratorModel : IGeneratorModel
{
    /// <summary>damage resolver intent id constant.</summary>
    public const string DamageResolverIntentId = "damage-resolver";
    /// <summary>health applier intent id constant.</summary>
    public const string HealthApplierIntentId = "health-applier";

    /// <summary>honest | buggy (damage-resolver only)</summary>
    public string Variant { get; set; } = "honest";

    /// <summary>Generate asynchronously.</summary>
    public Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(intent.IntentId.ToLowerInvariant() switch
        {
            var id when string.Equals(id, DamageResolverIntentId, StringComparison.Ordinal) =>
                new GeneratedBrickSource(
                    Variant switch
                    {
                        "buggy" => DamageResolverSources.Buggy(witnessSignature),
                        _ => DamageResolverSources.Honest(witnessSignature)
                    },
                    Provenance: $"cursor:{Variant}",
                    ClassName: "DamageResolverBrick",
                    Namespace: "Nexo.Certified.DamageResolver"),

            var id when string.Equals(id, HealthApplierIntentId, StringComparison.Ordinal) =>
                new GeneratedBrickSource(
                    HealthApplierSources.Honest(witnessSignature),
                    Provenance: "cursor:honest",
                    ClassName: "HealthApplierBrick",
                    Namespace: "Nexo.Certified.DamageResolver"),

            _ => throw new NotSupportedException($"Cursor model has no source for intent '{intent.IntentId}'.")
        });
    }
}
