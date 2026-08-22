using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions;
using Ashlar.Commercial.GameDomain.Mapping;

namespace GameDirector.Mcp.Forge;
/// <summary>
/// Runs heuristic analysis then optionally augments with <see cref="IModel"/> when enabled.
/// </summary>
public sealed class ModelAugmentedVectorMapIntelligenceService : IVectorMapIntelligenceService
{
    private readonly IModel _model;
    private readonly HeuristicVectorMapIntelligenceService _heuristic;
    private readonly IOptions<ForgeSessionOptions> _options;
    private readonly ILogger<ModelAugmentedVectorMapIntelligenceService> _log;

    public ModelAugmentedVectorMapIntelligenceService(
        IModel model,
        HeuristicVectorMapIntelligenceService heuristic,
        IOptions<ForgeSessionOptions> options,
        ILogger<ModelAugmentedVectorMapIntelligenceService> log)
    {
        _model = model;
        _heuristic = heuristic;
        _options = options;
        _log = log;
    }

    public async Task<VectorMapIntelligenceResult> AnalyzeAsync(
        ReadOnlyMemory<byte> rawBytes,
        string? contentTypeHint,
        CancellationToken cancellationToken = default)
    {
        var baseResult = await _heuristic.AnalyzeAsync(rawBytes, contentTypeHint, cancellationToken)
            .ConfigureAwait(false);

        if (!_options.Value.EnableVectorModel || rawBytes.Length == 0)
            return baseResult;

        var maxPromptChars = _options.Value.MaxVectorModelPromptChars > 0
            ? _options.Value.MaxVectorModelPromptChars
            : 4000;

        var snippet = string.Join("; ", baseResult.Notes);
        if (snippet.Length > maxPromptChars)
            snippet = snippet[..maxPromptChars] + "…";

        var prompt =
            "You are assisting with geographic vector map QA. Summarize risks or anomalies in one short paragraph. " +
            "Do not invent coordinates. Base analysis:\n" + snippet;

        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var ms = _options.Value.VectorModelTimeoutMs > 0
                ? _options.Value.VectorModelTimeoutMs
                : 15_000;
            linked.CancelAfter(ms);

            var output = await _model.CompleteAsync(
                new ModelInput([("user", prompt)]),
                linked.Token).ConfigureAwait(false);

            var modelText = (output.Text ?? string.Empty).Trim();
            if (modelText.Length > 2000)
                modelText = modelText[..2000] + "…";

            var merged = baseResult.Summary + " | model: " + modelText;
            var notes = new List<string>(baseResult.Notes) { "model_augmented: true" };
            return new VectorMapIntelligenceResult(merged, notes);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Vector map model augmentation failed; returning heuristic only.");
            return baseResult;
        }
    }
}
