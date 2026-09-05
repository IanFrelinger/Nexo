using Microsoft.Extensions.Logging;
using Ashlar.Brick.Contracts;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// DomainBrick registry that merges local bricks with bricks from one or more remote catalogs.
/// Remote bricks are returned as <see cref="RemoteBrick"/> instances.
/// </summary>
public sealed class CompositeBrickRegistry : IBrickRegistry
{
    private readonly IBrickRegistry _local;
    private readonly IReadOnlyList<IRemoteBrickCatalog> _remoteCatalogs;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CompositeBrickRegistry>? _logger;

    /// <summary>Initializes a new composite brick registry.</summary>
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

    /// <summary>Gets brick.</summary>
    public DomainBrick? GetBrick(string id)
    {
        var local = _local.GetBrick(id);
        if (local != null) return local;

        foreach (var catalog in _remoteCatalogs)
        {
            try
            {
                var entry = WaitOffSyncContext(() => catalog.GetByIdAsync(id));
                if (entry != null)
                {
                    var executeBaseUrl = entry.HostBaseUrl ?? catalog.BaseUrl.TrimEnd('/');
                    var capabilityFetch = WaitOffSyncContext(catalog.GetCapabilitiesWithStalenessAsync);
                    entry.HostCapabilities ??= capabilityFetch.Capabilities;
                    if (capabilityFetch.IsStale)
                    {
                        _logger?.LogWarning(
                            "Remote brick {BrickId} from {BaseUrl} is using stale host capability data.",
                            entry.Id,
                            catalog.BaseUrl);
                    }
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

    /// <summary>Gets all bricks.</summary>
    public IReadOnlyList<DomainBrick> GetAllBricks()
    {
        var set = new Dictionary<string, DomainBrick>(StringComparer.OrdinalIgnoreCase);

        foreach (var b in _local.GetAllBricks())
            set[b.Id] = b;

        foreach (var catalog in _remoteCatalogs)
        {
            try
            {
                var entries = WaitOffSyncContext(catalog.GetAllAsync);
                var baseUrl = catalog.BaseUrl.TrimEnd('/');
                var capabilityFetch = WaitOffSyncContext(catalog.GetCapabilitiesWithStalenessAsync);
                var hostCapabilities = capabilityFetch.Capabilities;
                if (capabilityFetch.IsStale)
                {
                    _logger?.LogWarning(
                        "Remote catalog {BaseUrl} returned stale host capability data for brick list.",
                        catalog.BaseUrl);
                }
                foreach (var entry in entries)
                {
                    if (set.ContainsKey(entry.Id)) continue;
                    entry.HostCapabilities ??= hostCapabilities;
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

    /// <summary>
    /// Catalog I/O is async; <see cref="IBrickRegistry"/> is sync. Blocking on the caller
    /// context deadlocks under ASP.NET. Offload to the thread pool so continuations
    /// cannot require the blocked request thread.
    /// </summary>
    private static T WaitOffSyncContext<T>(Func<Task<T>> start)
    {
        ArgumentNullException.ThrowIfNull(start);
        return Task.Run(start).GetAwaiter().GetResult();
    }
}
