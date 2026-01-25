using Nexo.GeoTerrain;

namespace Nexo.SDK.Estimation;

/// <summary>
/// Result wrapper that includes resource estimates along with the actual result.
/// </summary>
public sealed record EstimationResult<T>
{
    /// <summary>
    /// The actual operation result.
    /// </summary>
    public required T Result { get; init; }

    /// <summary>
    /// Estimated resource usage (before operation).
    /// </summary>
    public ResourceEstimate? Estimate { get; init; }

    /// <summary>
    /// Actual resource usage (after operation).
    /// </summary>
    public ResourceEstimate? Actual { get; init; }
}
