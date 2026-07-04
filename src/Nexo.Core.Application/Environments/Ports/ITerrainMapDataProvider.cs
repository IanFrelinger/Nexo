using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Provides elevation / terrain samples (raster heights, contour-adjacent grids, or DEM tiles).
/// Used for voxel terrain fill and vertical alignment. Self-hosted terrain tile APIs implement this.
/// </summary>
public interface ITerrainMapDataProvider
{
    /// <summary>Discriminator matching <see cref="MapDataSourceBinding.Kind"/> this implementation handles.</summary>
    string Kind { get; }

    /// <summary>
    /// Fetches terrain coverage for the geographic bounds at the requested resolution hint (metres per sample or zoom).
    /// </summary>
    /// <param name="bounds">Geographic rectangle to query.</param>
    /// <param name="binding">Data source connection details.</param>
    /// <param name="context">Cross-cutting request context.</param>
    /// <param name="resolutionMetersHint">Desired horizontal resolution in metres.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<TerrainMapDataResult> FetchAsync(
        MapDataGeographicBounds bounds,
        MapDataSourceBinding binding,
        MapDataRequestContext context,
        double resolutionMetersHint,
        CancellationToken cancellationToken = default);
}
