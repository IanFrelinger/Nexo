namespace Ashlar.Core.Application.Environments;

/// <summary>
/// Result of AI-assisted cleanup or enrichment of raw vector map text (OSM XML, GeoJSON, etc.).
/// Hosts may replace messy community data before voxel meshing or simulation.
/// </summary>
/// <param name="RefinedPayload">UTF-8 bytes of improved text; if unchanged, equals input.</param>
/// <param name="FormatHint">Same as request or normalized (e.g. osm-xml, geojson).</param>
/// <param name="Notes">Human-readable summary of fixes (tag harmonization, duplicate removal, etc.).</param>
/// <param name="WasModified">True when content differed from input.</param>
public sealed record VectorMapIntelligenceResult(
    ReadOnlyMemory<byte> RefinedPayload,
    string FormatHint,
    string? Notes,
    bool WasModified);
