namespace Nexo.Commercial.Fleet.Infrastructure;

/// <summary>
/// Director fleet + task store persistence (in-memory default; LiteDB for durable director).
/// </summary>
public sealed class MeshPersistenceOptions
{
    /// <summary>Constant value for section path.</summary>
    public const string SectionPath = "Nexo:Mesh:Persistence";

    /// <summary>Provider name: <c>InMemory</c> or <c>LiteDb</c>.</summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>LiteDB file path or connection string when <see cref="Provider"/> is LiteDb.</summary>
    public string DatabasePath { get; set; } = "mesh-director.db";

    /// <summary>Is lite db operation.</summary>
    public static bool IsLiteDb(string? provider) =>
        string.Equals(provider, "LiteDb", StringComparison.OrdinalIgnoreCase);

    /// <summary>Is known provider operation.</summary>
    public static bool IsKnownProvider(string? provider) =>
        string.IsNullOrWhiteSpace(provider) ||
        string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase) ||
        IsLiteDb(provider);
}
