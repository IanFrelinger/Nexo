using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation.Generation;

/// <summary>
/// <strong>TEST DOUBLE</strong> — hermetic stand-in for <see cref="ProviderGeneratorModel"/> in certification tests.
/// Returns canned source by intent id and variant; not production generation.
/// </summary>
public sealed class FixtureGeneratorModel : IGeneratorModel
{
    /// <summary>line substring counter intent id constant.</summary>
    public const string LineSubstringCounterIntentId = "line-substring-counter";

    /// <summary>correct | buggy | dependency-leak</summary>
    public string Variant { get; set; } = "correct";

    /// <summary>Generate asynchronously.</summary>
    public Task<GeneratedBrickSource> GenerateAsync(
        IntentSpec intent,
        WitnessSignature witnessSignature,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(intent.IntentId, LineSubstringCounterIntentId, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(intent.IntentId, "error-summary-extractor", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Fixture model has no canned source for intent '{intent.IntentId}'.");

        var source = intent.IntentId.ToLowerInvariant() switch
        {
            LineSubstringCounterIntentId => Variant switch
            {
                "buggy" => LineSubstringCounterSources.Buggy(witnessSignature),
                "dependency-leak" => LineSubstringCounterSources.DependencyLeak(witnessSignature),
                _ => LineSubstringCounterSources.Correct(witnessSignature)
            },
            "error-summary-extractor" => ErrorSummaryExtractorSources.Correct(witnessSignature),
            _ => throw new NotSupportedException($"Fixture model has no canned source for intent '{intent.IntentId}'.")
        };

        var className = intent.IntentId.ToLowerInvariant() switch
        {
            LineSubstringCounterIntentId => "LineSubstringCounterBrick",
            "error-summary-extractor" => "ErrorSummaryExtractorBrick",
            _ => "GeneratedBrick"
        };

        var ns = intent.IntentId.ToLowerInvariant() switch
        {
            "error-summary-extractor" => "ErrorSummaryExtractorBrick",
            _ => "Nexo.Certified.DamageResolver"
        };

        return Task.FromResult(new GeneratedBrickSource(
            source,
            Provenance: $"fixture:{Variant}",
            ClassName: className,
            Namespace: ns));
    }
}
