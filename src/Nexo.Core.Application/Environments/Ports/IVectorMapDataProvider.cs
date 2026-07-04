using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Provides vector map features (typically OSM-derived): roads, buildings, landuse polygons.
/// Implementations may call public APIs, <see cref="MapDataSourceBinding.BaseUrl"/> for a
/// self-hosted mirror, or local extracts. Hosts register the implementation keyed by
/// <see cref="MapDataSourceBinding.Kind"/>.
/// </summary>
public interface IVectorMapDataProvider
{
    /// <summary>Discriminator matching <see cref="MapDataSourceBinding.Kind"/> this implementation handles.</summary>
    string Kind { get; }

    /// <summary>
    /// Returns OSM XML or another vector payload for the bounding box. Format is described by
    /// <paramref name="formatHint"/> (e.g. <c>osm-xml</c>, <c>geojson</c>).
    /// </summary>
    Task<VectorMapDataResult> FetchAsync(
        MapDataGeographicBounds bounds,
        MapDataSourceBinding binding,
        MapDataRequestContext context,
        string formatHint,
        CancellationToken cancellationToken = default);
}
