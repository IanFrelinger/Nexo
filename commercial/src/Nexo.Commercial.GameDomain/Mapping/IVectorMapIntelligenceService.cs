namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Optional AI-assisted interpretation of raw vector map bytes (implemented by hosts or no-op).
/// </summary>
public interface IVectorMapIntelligenceService
{
    /// <summary>
    /// Produce a lightweight summary for telemetry or downstream refinement (must not block unbounded).
    /// </summary>
    Task<VectorMapIntelligenceResult> AnalyzeAsync(
        ReadOnlyMemory<byte> rawBytes,
        string? contentTypeHint,
        CancellationToken cancellationToken = default);
}
