namespace Nexo.GameDomain.Mapping;

/// <summary>
/// No-op implementation suitable for default API hosts.
/// </summary>
public sealed class NoOpVectorMapIntelligenceService : IVectorMapIntelligenceService
{
    public Task<VectorMapIntelligenceResult> AnalyzeAsync(
        ReadOnlyMemory<byte> rawBytes,
        string? contentTypeHint,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var n = rawBytes.Length;
        var summary = n == 0
            ? "No vector bytes to analyze."
            : $"Vector payload length {n} bytes (no-op intelligence).";
        return Task.FromResult(new VectorMapIntelligenceResult(summary, []));
    }
}
