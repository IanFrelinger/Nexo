namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Loads vector map files (OSM XML, GeoJSON) from disk under a caller-supplied root directory.
/// Paths are sandboxed to prevent traversal outside the caller-supplied root directory.
/// </summary>
public static class FileVectorMapSource
{
    /// <summary>
    /// Reads the file if it exists and passes sandbox checks. Does not decode XML/JSON.
    /// </summary>
    public static async Task<VectorMapFetchResult> TryLoadAsync(string rootPath, string relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(relativePath))
            return new VectorMapFetchResult(false, null, 0, "root and path are required");

        if (!TryResolveUnderRoot(rootPath, relativePath, out var full, out var error))
            return new VectorMapFetchResult(false, null, 0, error);

        if (!File.Exists(full))
            return new VectorMapFetchResult(false, null, 0, "file not found");

        var bytes = await File.ReadAllBytesAsync(full, ct).ConfigureAwait(false);
        var contentType = GuessContentType(relativePath);
        return new VectorMapFetchResult(true, contentType, bytes.Length, null);
    }

    /// <summary>
    /// Exposed for tests and host adapters that need the resolved absolute path after sandbox checks.
    /// </summary>
    public static bool TryResolveUnderRoot(string rootPath, string relativePath, out string fullPath, out string error)
    {
        fullPath = string.Empty;
        error = string.Empty;

        var trimmed = relativePath.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            error = "path is required";
            return false;
        }

        // Must reject rooted paths before stripping leading slashes ("/etc/passwd" would otherwise become relative).
        if (Path.IsPathRooted(trimmed))
        {
            error = "absolute paths not permitted";
            return false;
        }

        var normalized = trimmed.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            error = "path traversal not permitted";
            return false;
        }

        var rootFull = Path.GetFullPath(rootPath);
        var combined = Path.GetFullPath(Path.Combine(rootFull, normalized));
        var rootWithSep = rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(combined, rootFull, StringComparison.OrdinalIgnoreCase))
        {
            error = "resolved path escapes root";
            return false;
        }

        fullPath = combined;
        return true;
    }

    /// <summary>
    /// Reads the start of the file and guesses OSM vs GeoJSON for host adapters.
    /// </summary>
    public static async Task<VectorMapInspectResult> InspectAsync(string rootPath, string relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(relativePath))
            return new VectorMapInspectResult(false, null, 0, null, "root and path are required");

        if (!TryResolveUnderRoot(rootPath, relativePath, out var full, out var error))
            return new VectorMapInspectResult(false, null, 0, null, error);

        if (!File.Exists(full))
            return new VectorMapInspectResult(false, null, 0, null, "file not found");

        var len = (int)new FileInfo(full).Length;
        const int headMax = 32 * 1024;
        await using var stream = File.OpenRead(full);
        var buffer = new byte[Math.Min(headMax, len > 0 ? len : headMax)];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
        var head = buffer.AsMemory(0, read);

        var contentType = GuessContentType(relativePath);
        var format = SniffFormat(head.Span, relativePath);
        return new VectorMapInspectResult(true, contentType, len, format, null);
    }

    private static string GuessContentType(string relativePath)
    {
        var ext = Path.GetExtension(relativePath).ToLowerInvariant();
        return ext switch
        {
            ".osm" => "application/xml",
            ".xml" => "application/xml",
            ".geojson" => "application/geo+json",
            ".json" => "application/geo+json",
            _ => "application/octet-stream"
        };
    }

    private static string SniffFormat(ReadOnlySpan<byte> head, string relativePath)
    {
        var ext = Path.GetExtension(relativePath).ToLowerInvariant();
        if (ext is ".osm")
            return "osm_xml";

        var text = System.Text.Encoding.UTF8.GetString(head);
        var trimmed = text.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            if (trimmed.Contains("\"features\"", StringComparison.Ordinal) ||
                trimmed.Contains("\"FeatureCollection\"", StringComparison.Ordinal) ||
                trimmed.Contains("\"type\"", StringComparison.Ordinal))
                return "geojson";
            return "json_or_geojson";
        }

        if (trimmed.StartsWith('<') &&
            (trimmed.Contains("<osm", StringComparison.OrdinalIgnoreCase) ||
             trimmed.Contains("xmlns=\"http://openstreetmap.org/osm", StringComparison.OrdinalIgnoreCase)))
            return "osm_xml";

        return ext switch
        {
            ".geojson" => "geojson",
            ".json" => "json_or_geojson",
            _ => "unknown"
        };
    }
}
