using Microsoft.Extensions.Logging;
using Nexo.BrickContracts;
using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Brick registry that merges local bricks with bricks from one or more remote catalogs.
/// Remote bricks are returned as <see cref="RemoteBrick"/> instances.
/// </summary>
public sealed class CompositeBrickRegistry : IBrickRegistry
{
    private readonly IBrickRegistry _local;
    private readonly IReadOnlyList<IRemoteBrickCatalog> _remoteCatalogs;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CompositeBrickRegistry>? _logger;

    public CompositeBrickRegistry(
        IBrickRegistry local,
        IReadOnlyList<IRemoteBrickCatalog> remoteCatalogs,
        HttpClient httpClient,
        ILogger<CompositeBrickRegistry>? logger = null)
    {
        _local = local ?? throw new ArgumentNullException(nameof(local));
        _remoteCatalogs = remoteCatalogs ?? new List<IRemoteBrickCatalog>();
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    public Brick? GetBrick(string id)
    {
        var local = _local.GetBrick(id);
        if (local != null) return local;

        foreach (var catalog in _remoteCatalogs)
        {
            try
            {
                var entry = catalog.GetByIdAsync(id).GetAwaiter().GetResult();
                if (entry != null)
                {
                    var executeBaseUrl = entry.HostBaseUrl ?? catalog.BaseUrl.TrimEnd('/');
                    return new RemoteBrick(entry, _httpClient, executeBaseUrl, null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get brick {BrickId} from catalog {BaseUrl}", id, catalog.BaseUrl);
            }
        }

        return null;
    }

    public IReadOnlyList<Brick> GetAllBricks()
    {
        var set = new Dictionary<string, Brick>(StringComparer.OrdinalIgnoreCase);

        foreach (var b in _local.GetAllBricks())
            set[b.Id] = b;

        foreach (var catalog in _remoteCatalogs)
        {
            try
            {
                var entries = catalog.GetAllAsync().GetAwaiter().GetResult();
                var baseUrl = catalog.BaseUrl.TrimEnd('/');
                foreach (var entry in entries)
                {
                    if (set.ContainsKey(entry.Id)) continue;
                    set[entry.Id] = new RemoteBrick(entry, _httpClient, entry.HostBaseUrl ?? baseUrl, null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get bricks from catalog {BaseUrl}", catalog.BaseUrl);
            }
        }

        return set.Values.ToList();
    }
}
