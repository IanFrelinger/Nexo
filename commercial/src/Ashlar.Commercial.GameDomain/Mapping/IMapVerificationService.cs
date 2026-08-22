namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Optional hierarchical verification after vector payload inspection and summarization (rules / heuristics).
/// </summary>
public interface IMapVerificationService
{
    /// <summary>
    /// Runs checks on parse output; does not throw for heuristic failures.
    /// </summary>
    Task<MapVerificationResult> VerifyAsync(
        VectorPayloadInspection inspection,
        VectorMapParseSummary parseSummary,
        CancellationToken cancellationToken = default);
}
