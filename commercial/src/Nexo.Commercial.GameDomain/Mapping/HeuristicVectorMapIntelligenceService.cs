namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Heuristic vector map analysis (format sniff + snippet); no network or LLM.
/// </summary>
public sealed class HeuristicVectorMapIntelligenceService : IVectorMapIntelligenceService
{
    /// <summary>Analyze async operation.</summary>
    public Task<VectorMapIntelligenceResult> AnalyzeAsync(
        ReadOnlyMemory<byte> rawBytes,
        string? contentTypeHint,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var insp = VectorMapPayloadInspector.Inspect(rawBytes, contentTypeHint);
        var summary = rawBytes.Length == 0
            ? "Empty payload."
            : $"Format guess: {insp.FormatGuess}; {rawBytes.Length} bytes.";
        var notes = new List<string>(insp.Notes);
        if (!string.IsNullOrEmpty(insp.Snippet))
            notes.Add("payload_snippet: " + insp.Snippet);
        return Task.FromResult(new VectorMapIntelligenceResult(summary, notes));
    }
}
