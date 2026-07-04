namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Configuration for networked brick discovery (remote catalogs and optional auth).
/// </summary>
public class BrickHostOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "BrickHost";

    /// <summary>Base URLs of remote brick catalogs (GET /api/bricks).</summary>
    public IReadOnlyList<string> RemoteCatalogBaseUrls { get; set; } = [];

    /// <summary>Catalog cache TTL in seconds (0 = no cache).</summary>
    public int CatalogCacheTtlSeconds { get; set; } = 60;

    /// <summary>Use API key for catalog and execute requests.</summary>
    public bool UseAuth { get; set; }

    /// <summary>Header name for API key (e.g. "X-Api-Key").</summary>
    public string ApiKeyHeader { get; set; } = "X-Api-Key";

    /// <summary>API key value (from config or env; avoid secrets in appsettings).</summary>
    public string? ApiKeyValue { get; set; }
}
