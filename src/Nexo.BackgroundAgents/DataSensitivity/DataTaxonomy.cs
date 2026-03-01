using System.Collections.Concurrent;
using System.Text.Json;

namespace Nexo.BackgroundAgents.DataSensitivity;

/// <summary>
/// Default implementation of IDataTaxonomy. Reads from DataTaxonomy.json (versioned).
/// Falls back to TopSecret for unknown types.
/// </summary>
public sealed class DataTaxonomy : IDataTaxonomy
{
    private const string DefaultLevelForUnknown = "TopSecret";
    private readonly IReadOnlyDictionary<string, string?> _mappings;
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string?>> Cache = new();

    /// <summary>
    /// Creates a taxonomy from the given mappings.
    /// </summary>
    /// <param name="mappings">Data type -> level name. Null value means inherit from source.</param>
    public DataTaxonomy(IReadOnlyDictionary<string, string?>? mappings = null)
    {
        _mappings = mappings ?? LoadFromEmbeddedOrFile();
    }

    /// <inheritdoc />
    public string? GetDefaultLevelForDataType(string dataType)
    {
        if (string.IsNullOrWhiteSpace(dataType))
            return DefaultLevelForUnknown;

        var key = dataType.Trim();
        if (_mappings.TryGetValue(key, out var level))
            return level ?? DefaultLevelForUnknown;

        return DefaultLevelForUnknown;
    }

    private static IReadOnlyDictionary<string, string?> LoadFromEmbeddedOrFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "DataTaxonomy.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "DataTaxonomy.json"),
            Path.Combine(baseDir, "DataSensitivity", "DataTaxonomy.json"),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;

            var cacheKey = path + ":" + File.GetLastWriteTimeUtc(path).Ticks;
            return Cache.GetOrAdd(cacheKey, _ =>
            {
                try
                {
                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("mappings", out var map))
                        return new Dictionary<string, string?>();

                    var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in map.EnumerateObject())
                    {
                        var val = prop.Value.ValueKind == JsonValueKind.Null
                            ? null
                            : prop.Value.GetString();
                        dict[prop.Name] = val;
                    }
                    return dict;
                }
                catch
                {
                    return GetBuiltInMappings();
                }
            });
        }

        return GetBuiltInMappings();
    }

    private static IReadOnlyDictionary<string, string?> GetBuiltInMappings()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["editor-events"] = "Public",
            ["file-paths"] = "TopSecret",
            ["file-contents"] = "TopSecret",
            ["git-metadata"] = "Public",
            ["api-keys"] = "Secret",
            ["tokens"] = "Secret",
            ["terminal-output"] = "TopSecret",
            ["process-names"] = "Public",
            ["behavioral-patterns"] = "TopSecret",
        };
    }
}
