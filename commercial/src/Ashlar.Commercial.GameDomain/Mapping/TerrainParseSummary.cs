namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Lightweight terrain raster inspection result (PNG/JPEG/TIFF hints; no full decode in Ashlar.Commercial.GameDomain).
/// </summary>
public sealed record TerrainParseSummary(
    string ParserKind,
    string Summary,
    IReadOnlyList<string> Details);
