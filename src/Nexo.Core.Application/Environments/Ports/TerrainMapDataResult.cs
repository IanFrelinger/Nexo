using Nexo.Core.Application.Environments;

namespace Nexo.Core.Application.Environments.Ports;

/// <summary>
/// Terrain map fetch result from an <see cref="ITerrainMapDataProvider"/>.
/// </summary>
/// <param name="Payload">Raw payload (GeoTIFF bytes, heightfield binary, JSON metadata + blob URL, etc.).</param>
/// <param name="ContentType">MIME-like hint.</param>
/// <param name="ResolutionMeters">Effective horizontal resolution if known.</param>
/// <param name="SourceDescription">Optional provenance.</param>
public sealed record TerrainMapDataResult(
    ReadOnlyMemory<byte> Payload,
    string ContentType,
    double? ResolutionMeters = null,
    string? SourceDescription = null);
