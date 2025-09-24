using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Networking.Interfaces;
using Nexo.Feature.Networking.Models;

namespace Nexo.Feature.Networking.Services;

/// <summary>
/// Orchestrates networking configuration across multiple providers
/// </summary>
public class NetworkingOrchestrator
{
    private readonly ILogger<NetworkingOrchestrator> _logger;
    private readonly IEnumerable<INetworkingProvider> _providers;
    private readonly Dictionary<string, INetworkingProvider> _providerMap;

    public NetworkingOrchestrator(
        ILogger<NetworkingOrchestrator> logger,
        IEnumerable<INetworkingProvider> providers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _providerMap = _providers.ToDictionary(p => p.ProviderId, p => p);
    }

    /// <summary>
    /// Generates networking configuration using the best available provider
    /// </summary>
    public async Task<NetworkingResult> GenerateNetworkingConfigurationAsync(
        NetworkingRequest request, 
        string? preferredProviderId = null,
        CancellationToken cancellationToken = default)
    {
        var provider = SelectProvider(request, preferredProviderId);
        
        if (provider == null)
        {
            return new NetworkingResult
            {
                Success = false,
                Error = "No suitable networking provider available"
            };
        }

        _logger.LogInformation("Using provider {ProviderId} for networking configuration", provider.ProviderId);
        return await provider.GenerateNetworkingConfigurationAsync(request, cancellationToken);
    }

    /// <summary>
    /// Generates networking configuration using a specific provider
    /// </summary>
    public async Task<NetworkingResult> GenerateNetworkingConfigurationAsync(
        string providerId,
        NetworkingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_providerMap.TryGetValue(providerId, out var provider))
        {
            return new NetworkingResult
            {
                Success = false,
                Error = $"Provider {providerId} not found"
            };
        }

        if (!provider.IsAvailable)
        {
            return new NetworkingResult
            {
                Success = false,
                Error = $"Provider {providerId} is not available"
            };
        }

        return await provider.GenerateNetworkingConfigurationAsync(request, cancellationToken);
    }

    /// <summary>
    /// Generates multiple networking configurations using the best available provider
    /// </summary>
    public async Task<IEnumerable<NetworkingResult>> GenerateNetworkingBatchAsync(
        IEnumerable<NetworkingRequest> requests,
        string? preferredProviderId = null,
        CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return Enumerable.Empty<NetworkingResult>();
        }

        var provider = SelectProvider(requestList.First(), preferredProviderId);
        
        if (provider == null)
        {
            return requestList.Select(_ => new NetworkingResult
            {
                Success = false,
                Error = "No suitable networking provider available"
            });
        }

        _logger.LogInformation("Using provider {ProviderId} for batch networking configuration", provider.ProviderId);
        return await provider.GenerateNetworkingBatchAsync(requestList, cancellationToken);
    }

    /// <summary>
    /// Gets provider statistics
    /// </summary>
    public Task<Dictionary<string, object>> GetProviderStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new Dictionary<string, object>();
        
        foreach (var provider in _providers)
        {
            try
            {
                stats[provider.ProviderId] = new
                {
                    DisplayName = provider.DisplayName,
                    IsAvailable = provider.IsAvailable,
                    RequiresOnline = provider.RequiresOnline
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get stats for provider {ProviderId}", provider.ProviderId);
                stats[provider.ProviderId] = new
                {
                    DisplayName = provider.DisplayName,
                    IsAvailable = false,
                    RequiresOnline = provider.RequiresOnline,
                    Error = ex.Message
                };
            }
        }

        return Task.FromResult(stats);
    }

    private INetworkingProvider? SelectProvider(NetworkingRequest request, string? preferredProviderId = null)
    {
        // If a specific provider is requested, try to use it
        if (!string.IsNullOrEmpty(preferredProviderId) && 
            _providerMap.TryGetValue(preferredProviderId, out var preferredProvider) &&
            preferredProvider.IsAvailable)
        {
            return preferredProvider;
        }

        // Select the best available provider based on request requirements
        var availableProviders = _providers.Where(p => p.IsAvailable).ToList();
        
        if (!availableProviders.Any())
        {
            return null;
        }

        // Prefer offline providers for basic requests
        if (request.NetworkingType == NetworkingType.ClientServer || request.MaxPlayers <= 8)
        {
            var offlineProvider = availableProviders.FirstOrDefault(p => !p.RequiresOnline);
            if (offlineProvider != null)
            {
                return offlineProvider;
            }
        }

        // For complex requests, prefer online providers
        if (request.MaxPlayers > 16 || request.NetworkingType == NetworkingType.CloudMultiplayer)
        {
            var onlineProvider = availableProviders.FirstOrDefault(p => p.RequiresOnline);
            if (onlineProvider != null)
            {
                return onlineProvider;
            }
        }

        // Default to first available provider
        return availableProviders.First();
    }
}
