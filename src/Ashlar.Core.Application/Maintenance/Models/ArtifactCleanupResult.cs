namespace Ashlar.Core.Application.Maintenance.Models;

/// <summary>
/// Result of artifact cleanup execution.
/// </summary>
/// <param name="StrategyId">ID of the strategy that produced this result.</param>
/// <param name="BytesReclaimed">Total bytes reclaimed (approximate).</param>
/// <param name="PathsDeleted">Paths or directories that were deleted.</param>
/// <param name="Errors">Any errors encountered during cleanup (non-fatal; best-effort).</param>
public sealed record ArtifactCleanupResult(
    string StrategyId,
    long BytesReclaimed,
    IReadOnlyList<string> PathsDeleted,
    IReadOnlyList<string> Errors);
