namespace Nexo.Core.Application.Environments;

/// <summary>
/// Request for <see cref="Ports.IVectorMapIntelligenceService"/> (messy OSM/GeoJSON repair, tag cleanup).
/// </summary>
/// <param name="RawPayload">Source bytes (typically UTF-8 text).</param>
/// <param name="FormatHint">e.g. <c>osm-xml</c>, <c>geojson</c>.</param>
/// <param name="Context">Correlation and opaque hints for the implementation.</param>
/// <param name="MaxOutputBytes">Soft cap so embedded models stay bounded (implementations may truncate).</param>
public sealed record VectorMapIntelligenceRequest(
    ReadOnlyMemory<byte> RawPayload,
    string FormatHint,
    MapDataRequestContext Context,
    int MaxOutputBytes = 512 * 1024);
