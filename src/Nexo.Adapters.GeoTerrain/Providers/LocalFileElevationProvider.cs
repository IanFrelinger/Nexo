using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Loads SRTM tiles from a local directory (air-gapped friendly).
/// Files are expected to be named like <c>N37W122.hgt</c>.
/// </summary>
public sealed class LocalFileElevationProvider : IElevationProvider
{
    private readonly string _root;

    public string RootPath => _root;

    public LocalFileElevationProvider(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("LocalRootPath must be provided for Local elevation provider.", nameof(rootPath));
        _root = rootPath!;
    }

    public async Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        var fileName = tileId + ".hgt";
        var full = Path.Combine(_root, fileName);

        var bytes = await File.ReadAllBytesAsync(full, cancellationToken);
        var summary = SrtmHgtParser.Analyze(bytes);

        return new ElevationTile
        {
            TileId = tileId,
            HgtBytes = bytes,
            Size = summary.Size,
            MinMeters = summary.MinMeters,
            MaxMeters = summary.MaxMeters,
            NoDataSamples = summary.NoDataSamples,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "local",
                ["path"] = full
            }
        };
    }
}

