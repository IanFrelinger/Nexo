namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Request to execute (or simulate) the map adaptation pipeline.
/// </summary>
/// <param name="DryRun">When true, dry-run only.</param>
/// <param name="TimeoutMs">HTTP timeout for fetch stages.</param>
/// <param name="VectorDataUrl">Optional URL for vector data.</param>
/// <param name="TerrainDataUrl">Optional URL for terrain data.</param>
/// <param name="MvtTileZoom">Zoom for MVT when tile X/Y are explicit, or fallback when URL has no z/x/y.</param>
/// <param name="MvtTileX">Optional MVT column (overrides URL parsing).</param>
/// <param name="MvtTileY">Optional MVT row (overrides URL parsing).</param>
public sealed record MapPipelineRunRequest(
    bool DryRun = true,
    int TimeoutMs = 30_000,
    string? VectorDataUrl = null,
    string? TerrainDataUrl = null,
    int MvtTileZoom = VectorMapPayloadSummarizer.DefaultMvtTileZoom,
    int? MvtTileX = null,
    int? MvtTileY = null);
