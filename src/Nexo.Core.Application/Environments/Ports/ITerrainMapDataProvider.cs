using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Provides elevation / terrain samples (raster heights, contour-adjacent grids, or DEM tiles).
/// Used for voxel terrain fill and vertical alignment. Self-hosted terrain tile APIs implement this.
/// </summary>
public interface ITerrainMapDataProvider
{
    string Kind { get; }

    /// <summary>
    /// Fetches terrain coverage for the geographic bounds at the requested resolution hint (metres per sample or zoom).
    /// </summary>
    Task<TerrainMapDataResult> FetchAsync(
        MapDataGeographicBounds bounds,
        MapDataSourceBinding binding,
        MapDataRequestContext context,
        double resolutionMetersHint,
        CancellationToken cancellationToken = default);
}

/// <param name="Payload">Raw payload (GeoTIFF bytes, heightfield binary, JSON metadata + blob URL, etc.).</param>
/// <param name="ContentType">MIME-like hint.</param>
/// <param name="ResolutionMeters">Effective horizontal resolution if known.</param>
/// <param name="SourceDescription">Optional provenance.</param>
public sealed record TerrainMapDataResult(
    ReadOnlyMemory<byte> Payload,
    string ContentType,
    double? ResolutionMeters = null,
    string? SourceDescription = null);
