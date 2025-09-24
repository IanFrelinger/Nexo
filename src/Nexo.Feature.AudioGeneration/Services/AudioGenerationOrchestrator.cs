using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AudioGeneration.Interfaces;
using Nexo.Feature.AudioGeneration.Models;

namespace Nexo.Feature.AudioGeneration.Services;

/// <summary>
/// Orchestrates audio generation across multiple providers
/// </summary>
public class AudioGenerationOrchestrator
{
    private readonly ILogger<AudioGenerationOrchestrator> _logger;
    private readonly IEnumerable<IAudioGenerationProvider> _providers;
    private readonly Dictionary<string, IAudioGenerationProvider> _providerMap;

    public AudioGenerationOrchestrator(
        ILogger<AudioGenerationOrchestrator> logger,
        IEnumerable<IAudioGenerationProvider> providers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _providerMap = _providers.ToDictionary(p => p.ProviderId, p => p);
    }

    /// <summary>
    /// Gets all available audio generation models across all providers
    /// </summary>
    public async Task<IEnumerable<AudioGenerationModel>> GetAllAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var allModels = new List<AudioGenerationModel>();
        
        foreach (var provider in _providers.Where(p => p.IsAvailable))
        {
            try
            {
                var models = await provider.GetAvailableModelsAsync(cancellationToken);
                allModels.AddRange(models);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get models from provider {ProviderId}", provider.ProviderId);
            }
        }

        return allModels;
    }

    /// <summary>
    /// Generates audio using the best available provider
    /// </summary>
    public async Task<AudioGenerationResult> GenerateAudioAsync(
        AudioGenerationRequest request, 
        string? preferredProviderId = null,
        CancellationToken cancellationToken = default)
    {
        var provider = SelectProvider(request, preferredProviderId);
        
        if (provider == null)
        {
            return new AudioGenerationResult
            {
                Success = false,
                Error = "No suitable audio generation provider available"
            };
        }

        _logger.LogInformation("Using provider {ProviderId} for audio generation", provider.ProviderId);
        return await provider.GenerateAudioAsync(request, cancellationToken);
    }

    /// <summary>
    /// Generates audio using a specific provider
    /// </summary>
    public async Task<AudioGenerationResult> GenerateAudioAsync(
        string providerId,
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_providerMap.TryGetValue(providerId, out var provider))
        {
            return new AudioGenerationResult
            {
                Success = false,
                Error = $"Provider {providerId} not found"
            };
        }

        if (!provider.IsAvailable)
        {
            return new AudioGenerationResult
            {
                Success = false,
                Error = $"Provider {providerId} is not available"
            };
        }

        return await provider.GenerateAudioAsync(request, cancellationToken);
    }

    /// <summary>
    /// Generates multiple audio clips using the best available provider
    /// </summary>
    public async Task<IEnumerable<AudioGenerationResult>> GenerateAudioBatchAsync(
        IEnumerable<AudioGenerationRequest> requests,
        string? preferredProviderId = null,
        CancellationToken cancellationToken = default)
    {
        var requestList = requests.ToList();
        if (!requestList.Any())
        {
            return Enumerable.Empty<AudioGenerationResult>();
        }

        var provider = SelectProvider(requestList.First(), preferredProviderId);
        
        if (provider == null)
        {
            return requestList.Select(_ => new AudioGenerationResult
            {
                Success = false,
                Error = "No suitable audio generation provider available"
            });
        }

        _logger.LogInformation("Using provider {ProviderId} for batch audio generation", provider.ProviderId);
        return await provider.GenerateAudioBatchAsync(requestList, cancellationToken);
    }

    /// <summary>
    /// Gets provider statistics
    /// </summary>
    public async Task<Dictionary<string, object>> GetProviderStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new Dictionary<string, object>();
        
        foreach (var provider in _providers)
        {
            try
            {
                var models = await provider.GetAvailableModelsAsync(cancellationToken);
                stats[provider.ProviderId] = new
                {
                    DisplayName = provider.DisplayName,
                    IsAvailable = provider.IsAvailable,
                    RequiresOnline = provider.RequiresOnline,
                    ModelCount = models.Count(),
                    Models = models.Select(m => new { m.Id, m.Name, m.IsAvailable })
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

        return stats;
    }

    private IAudioGenerationProvider? SelectProvider(AudioGenerationRequest request, string? preferredProviderId = null)
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
        if (request.AudioType == AudioType.UI || request.Duration <= 5.0f)
        {
            var offlineProvider = availableProviders.FirstOrDefault(p => !p.RequiresOnline);
            if (offlineProvider != null)
            {
                return offlineProvider;
            }
        }

        // For longer or more complex requests, prefer online providers
        if (request.Duration > 10.0f || request.AudioType == AudioType.Music)
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
