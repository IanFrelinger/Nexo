namespace Nexo.BrickCatalog;

/// <summary>
/// Configuration for the central brick catalog (instance URLs to aggregate).
/// </summary>
public class CentralCatalogOptions
{
    public const string SectionName = "CentralCatalog";

    /// <summary>Base URLs of brick host instances (each must expose GET /api/bricks and GET /api/bricks/{id}).</summary>
    public IReadOnlyList<string> InstanceBaseUrls { get; set; } = [];

    /// <summary>Cache TTL in seconds (0 = no cache, fetch on every request).</summary>
    public int CacheTtlSeconds { get; set; } = 60;
}
