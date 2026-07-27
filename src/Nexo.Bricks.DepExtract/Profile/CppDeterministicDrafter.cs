using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Profile <see cref="IDeterministicDrafter"/> over <see cref="AdapterScaffold"/>.
/// Attaches scaffold provenance (assumptions / confidence) on the artifact.
/// </summary>
public sealed class CppDeterministicDrafter : IDeterministicDrafter
{
    private const double DefaultTemplateEchoThreshold = 0.15;

    /// <inheritdoc />
    public bool TryDraft(GenerationRequest request, out GeneratedArtifact? artifact)
    {
        artifact = null;
        if (request is null || string.IsNullOrWhiteSpace(request.Grounding))
            return false;

        if (!AdapterScaffold.LooksSimple(request.Grounding))
            return false;

        string? entryInclude = null;
        if (request.Overrides.TryGetValue("entryInclude", out var raw) && raw is string s
            && !string.IsNullOrWhiteSpace(s))
        {
            entryInclude = s;
        }

        var scaffold = AdapterScaffold.TryBuild(request.Grounding, entryInclude);
        if (scaffold is null)
            return false;

        var coverage = CppAdapterPrompt.SourceApiCoverage(scaffold, request.Grounding);
        var threshold = DefaultTemplateEchoThreshold;
        if (request.Overrides.TryGetValue("templateEchoThreshold", out var t) && t is double d)
            threshold = d;

        var echo = coverage < threshold;
        var warnings = new List<string>();
        if (echo)
            warnings.Add($"low source coverage ({coverage:P0}) — scaffold may not reference the real API");

        // Unverified until GxxCompileValidator / repair path confirms compile.
        warnings.Add(
            "unverified scaffold — not checked against the real parser; assumes a `long&`-out pull " +
            "method, millisecond timestamps, and a 1e300 t_end sentinel. Enable compile validation or review before use");

        artifact = new GeneratedArtifact
        {
            Content = scaffold,
            Provenance = GenerativeProvenance.Create(
                GenerationStrategy.Templated,
                verified: false,
                confidence: echo ? 0.3 : 0.55,
                warnings: warnings,
                assumptions: new[]
                {
                    "pull method takes a long& out-param",
                    "file-backed timestamps are millisecond ticks",
                    "t_end opened to a 1e300 sentinel"
                },
                unsupportedReferences: Array.Empty<string>(),
                requiresHumanReview: true)
        };
        return true;
    }
}
