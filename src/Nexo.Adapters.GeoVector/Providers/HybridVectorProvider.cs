using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;

namespace Nexo.Adapters.GeoVector.Providers;

/// <summary>
/// Hybrid routing provider: tries an online provider when available, falls back to offline provider.
/// </summary>
public sealed class HybridVectorProvider : IVectorProvider
{
    private readonly IVectorProvider _offline;
    private readonly IVectorProvider? _online;
    private readonly ILogger<HybridVectorProvider> _logger;

    public HybridVectorProvider(IVectorProvider offline, IVectorProvider? online = null, ILogger<HybridVectorProvider>? logger = null)
    {
        _offline = offline ?? throw new ArgumentNullException(nameof(offline));
        _online = online;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<HybridVectorProvider>.Instance;
    }

    public async Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        bounds.Validate();
        kind ??= FeatureKind.Building;

        var online = _online;
        if (online != null && NetworkInterface.GetIsNetworkAvailable())
        {
            try
            {
                return await online.GetFeaturesAsync(bounds, kind, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Online vector provider failed; falling back to offline. kind={Kind}", kind.Value);
            }
        }

        return await _offline.GetFeaturesAsync(bounds, kind, cancellationToken);
    }
}

