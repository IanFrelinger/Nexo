namespace Nexo.GameDomain.Mapping;

/// <summary>
/// A row in a web Mercator tile pyramid for map streaming (see <see cref="MapLodPyramidPlanner"/>).
/// </summary>
public sealed record MapTilePyramidTier(
    int LodLevel,
    double DetailFactor,
    int Zoom,
    int FinestZoom,
    int ZoomDeltaFromFinest,
    double ApproxAreaMultiplierVsFinest);
