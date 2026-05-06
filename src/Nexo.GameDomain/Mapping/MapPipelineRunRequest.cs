namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Request to execute (or simulate) the map adaptation pipeline.
/// </summary>
/// <param name="DryRun">When true, dry-run only.</param>
/// <param name="TimeoutMs">HTTP timeout for fetch stages.</param>
/// <param name="VectorDataUrl">Optional URL for vector data.</param>
/// <param name="TerrainDataUrl">Optional URL for terrain data.</param>
/// <param name="MvtTileZoom">Zoom level for MVT projection context (0–22); ignored for non-MVT payloads.</param>
public sealed record MapPipelineRunRequest(
    bool DryRun = true,
    int TimeoutMs = 30_000,
    string? VectorDataUrl = null,
    string? TerrainDataUrl = null,
    int MvtTileZoom = VectorMapPayloadSummarizer.DefaultMvtTileZoom);
