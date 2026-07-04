namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Lightweight terrain raster inspection result (PNG/JPEG/TIFF hints; no full decode in Nexo.Commercial.GameDomain).
/// </summary>
public sealed record TerrainParseSummary(
    string ParserKind,
    string Summary,
    IReadOnlyList<string> Details);
