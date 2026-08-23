using System.Globalization;

namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Stores raw tile responses under <c>&lt;root&gt;/&lt;aesthetic&gt;/&lt;provider&gt;/z/x/y.bin</c>.
/// </summary>
public sealed class MapTileDiskCache
{
    private readonly string _root;

    public MapTileDiskCache(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _root = Path.GetFullPath(rootDirectory.Trim());
    }

    /// <summary>Absolute path for the cache entry.</summary>
    public string GetFullPath(MapTileCacheKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Path.Combine(
            _root,
            key.AestheticId,
            key.ProviderId,
            key.Z.ToString(CultureInfo.InvariantCulture),
            key.X.ToString(CultureInfo.InvariantCulture),
            key.Y.ToString(CultureInfo.InvariantCulture) + ".bin");
    }

    /// <summary>Returns cached bytes or null if missing.</summary>
    public async Task<byte[]?> TryReadAsync(MapTileCacheKey key, CancellationToken cancellationToken = default)
    {
        var path = GetFullPath(key);
        if (!File.Exists(path))
            return null;
        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes atomically via temp file rename.</summary>
    public async Task WriteAsync(MapTileCacheKey key, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var path = GetFullPath(key);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmp = path + ".tmp";
        await File.WriteAllBytesAsync(tmp, data.ToArray(), cancellationToken).ConfigureAwait(false);
        File.Move(tmp, path, overwrite: true);
    }
}
