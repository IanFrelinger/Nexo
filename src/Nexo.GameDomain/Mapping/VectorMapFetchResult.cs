namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Result of loading raw vector map bytes (OSM XML, GeoJSON) from a file path.
/// </summary>
public sealed record VectorMapFetchResult(
    bool Ok,
    string? ContentType,
    int ByteLength,
    string? Error);
