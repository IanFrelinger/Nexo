namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Lightweight terrain raster inspection result (PNG/JPEG/TIFF hints; no full decode in Nexo.GameDomain).
/// </summary>
public sealed record TerrainParseSummary(
    string ParserKind,
    string Summary,
    IReadOnlyList<string> Details);
