using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Materials;

namespace Nexo.API.Forge;

/// <summary>
/// Runs heuristic material hints then optionally augments with <see cref="IModel"/> when enabled.
/// </summary>
public sealed class ModelAugmentedMaterialIntelligenceService : IMaterialIntelligenceService
{
    private readonly IModel _model;
    private readonly HeuristicMaterialIntelligenceService _heuristic;
    private readonly IOptions<ForgeSessionOptions> _options;
    private readonly ILogger<ModelAugmentedMaterialIntelligenceService> _log;

    public ModelAugmentedMaterialIntelligenceService(
        IModel model,
        HeuristicMaterialIntelligenceService heuristic,
        IOptions<ForgeSessionOptions> options,
        ILogger<ModelAugmentedMaterialIntelligenceService> log)
    {
        _model = model;
        _heuristic = heuristic;
        _options = options;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<MaterialSuggestionResult> SuggestAsync(
        AestheticPack pack,
        string? vectorParseKind,
        CancellationToken cancellationToken = default)
    {
        var baseResult = await _heuristic.SuggestAsync(pack, vectorParseKind, cancellationToken)
            .ConfigureAwait(false);

        if (!_options.Value.EnableMaterialModel)
            return baseResult;

        var maxPromptChars = _options.Value.MaxMaterialModelPromptChars > 0
            ? _options.Value.MaxMaterialModelPromptChars
            : 3000;

        var hintsCompact = string.Join("; ",
            baseResult.Hints.Select(h => $"{h.Role}={h.ColorOrParameterHint}"));
        var snippet = baseResult.Summary + " | " + hintsCompact;
        if (snippet.Length > maxPromptChars)
            snippet = snippet[..maxPromptChars] + "…";

        var prompt =
            "You assist game environment stylization. Given heuristic surface bindings, respond with one short paragraph " +
            "describing how to tune roughness, normals, or layering for stylized terrain/buildings. Do not invent URLs.\n" +
            snippet;

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ms = _options.Value.MaterialModelTimeoutMs > 0
                ? _options.Value.MaterialModelTimeoutMs
                : 15_000;
            linked.CancelAfter(ms);

            var output = await _model.CompleteAsync(
                new ModelInput([("user", prompt)]),
                linked.Token).ConfigureAwait(false);

            var modelText = (output.Text ?? string.Empty).Trim();
            if (modelText.Length > 2000)
                modelText = modelText[..2000] + "…";

            var mergedSummary = baseResult.Summary + " | model: " + modelText;
            var augmented = new List<MaterialSurfaceHint>(baseResult.Hints)
            {
                new MaterialSurfaceHint(
                    "model_notes",
                    modelText,
                    "Optional LLM augmentation when Nexo:ForgeSession:EnableMaterialModel is true.")
            };
            return new MaterialSuggestionResult(mergedSummary, augmented);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Material model augmentation failed; returning heuristic only.");
            return baseResult;
        }
    }
}
