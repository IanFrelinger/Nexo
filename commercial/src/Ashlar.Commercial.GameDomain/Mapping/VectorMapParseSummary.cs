namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Lightweight parse statistics for vector payloads (not full geometry processing).
/// </summary>
/// <param name="ParserKind">High-level parser used (<c>geojson</c>, <c>osm_xml</c>, <c>mvt</c>, <c>skipped</c>, <c>error</c>).</param>
/// <param name="Summary">Single-line description for pipeline logs.</param>
/// <param name="Details">Optional extra lines (layer names, top tags, etc.).</param>
public sealed record VectorMapParseSummary(string ParserKind, string Summary, IReadOnlyList<string> Details);
