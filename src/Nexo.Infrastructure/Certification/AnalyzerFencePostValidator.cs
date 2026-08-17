#pragma warning disable NEXOEXP001 // The gate enforces the experimental autonomy contract (touch-set) by design; see docs/SdkCompatibilityPolicy.md.
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Runs the analyzer fence during GENERATION, as an <see cref="IPostValidator{T}"/> the
/// host composes into brick-producing profiles (the deferred generation-side half of the
/// analyzer-gate series): the proposer gets the same verbatim, located feedback the
/// certification gate would give — several repair iterations earlier and far cheaper
/// than a witness/mutation run.
///
/// The AUTHORITY is unchanged: certification re-runs the fence regardless (A1.1's
/// gate-runner monopoly), so this validator can only make candidates better before they
/// get there, never admit anything. Host-composed rather than engine-prepended because
/// the generation engine is compiler-free by design; profiles that produce C# bricks opt
/// in with their compilation references.
/// </summary>
public sealed class AnalyzerFencePostValidator : IPostValidator<GeneratedArtifact>
{
    private readonly AnalyzerFenceGate _gate = new();
    private readonly IReadOnlyList<string> _compilationReferences;
    private readonly BrickConstraintManifest? _manifest;
    private readonly TouchSet? _touchSet;

    /// <summary>
    /// Creates the validator for one profile. Pass the SAME manifest and touch-set
    /// instances the profile's prompt and tier were derived from (A2.3/R1.3 single-source
    /// discipline holds across generation and certification alike).
    /// </summary>
    public AnalyzerFencePostValidator(
        IEnumerable<string> compilationReferences,
        BrickConstraintManifest? manifest = null,
        TouchSet? touchSet = null)
    {
        _compilationReferences = (compilationReferences ?? throw new ArgumentNullException(nameof(compilationReferences)))
            .ToArray();
        _manifest = manifest;
        _touchSet = touchSet;
    }

    /// <inheritdoc />
    public async ValueTask<(bool ok, string? reason)> ValidateAsync(GeneratedArtifact output, CancellationToken ct)
    {
        var source = output?.Content ?? "";
        if (string.IsNullOrWhiteSpace(source))
            return (false, "Candidate artifact is empty — produce the artifact content.");

        var outcome = await _gate.EvaluateAsync(source, _compilationReferences, _manifest, _touchSet, ct)
            .ConfigureAwait(false);

        return outcome.Passed
            ? (true, null)
            : (false, outcome.FormatProposerFeedback());
    }
}
