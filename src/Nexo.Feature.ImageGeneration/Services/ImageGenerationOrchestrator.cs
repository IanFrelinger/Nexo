using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.ImageGeneration.Interfaces;
using Nexo.Feature.ImageGeneration.Models;

namespace Nexo.Feature.ImageGeneration.Services;

/// <summary>
/// Orchestrator for image generation services that manages multiple providers
/// </summary>
public class ImageGenerationOrchestrator
{
    private readonly ILogger<ImageGenerationOrchestrator> _logger;
    private readonly List<IImageGenerationProvider> _providers;
    private readonly ImageGenerationConfiguration _configuration;

    public ImageGenerationOrchestrator(
        ILogger<ImageGenerationOrchestrator> logger,
        IEnumerable<IImageGenerationProvider> providers,
        ImageGenerationConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Generates images using the best available provider
    /// </summary>
    public async Task<ImageGenerationResult> GenerateImagesAsync(ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting image generation orchestration for prompt: {Prompt}", request.Prompt);

            // Select the best provider based on configuration and availability
            var provider = await SelectBestProviderAsync(request, cancellationToken);
            if (provider == null)
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = "No suitable image generation provider available"
                };
            }

            _logger.LogInformation("Selected provider: {Provider} for image generation", provider.DisplayName);

            // Generate images using the selected provider
            var result = await provider.GenerateImagesAsync(request, cancellationToken);
            
            // Add orchestrator metadata
            result = result with
            {
                Metadata = result.Metadata with
                {
                    Provider = $"{result.Metadata.Provider} (via orchestrator)"
                }
            };

            _logger.LogInformation("Image generation completed successfully with {ImageCount} images", result.Images.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in image generation orchestration");
            return new ImageGenerationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Generates images using a specific provider
    /// </summary>
    public async Task<ImageGenerationResult> GenerateImagesAsync(string providerId, ImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderId == providerId);
            if (provider == null)
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = $"Provider '{providerId}' not found"
                };
            }

            if (!provider.IsAvailable)
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    Error = $"Provider '{providerId}' is not available"
                };
            }

            _logger.LogInformation("Generating images with specific provider: {Provider}", provider.DisplayName);
            return await provider.GenerateImagesAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating images with provider {ProviderId}", providerId);
            return new ImageGenerationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets all available providers and their models
    /// </summary>
    public async Task<IEnumerable<ProviderInfo>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providerInfos = new List<ProviderInfo>();

        foreach (var provider in _providers)
        {
            try
            {
                var health = await provider.GetHealthAsync(cancellationToken);
                var models = await provider.GetAvailableModelsAsync(cancellationToken);

                providerInfos.Add(new ProviderInfo
                {
                    ProviderId = provider.ProviderId,
                    DisplayName = provider.DisplayName,
                    RequiresOnline = provider.RequiresOnline,
                    IsAvailable = provider.IsAvailable,
                    IsHealthy = health.IsHealthy,
                    HealthStatus = health.Status,
                    AvailableModels = models.ToList(),
                    LastHealthCheck = health.LastChecked
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting info for provider {ProviderId}", provider.ProviderId);
                providerInfos.Add(new ProviderInfo
                {
                    ProviderId = provider.ProviderId,
                    DisplayName = provider.DisplayName,
                    RequiresOnline = provider.RequiresOnline,
                    IsAvailable = false,
                    IsHealthy = false,
                    HealthStatus = $"Error: {ex.Message}",
                    AvailableModels = new List<ImageGenerationModel>(),
                    LastHealthCheck = DateTime.UtcNow
                });
            }
        }

        return providerInfos;
    }

    /// <summary>
    /// Gets health status of all providers
    /// </summary>
    public async Task<ImageGenerationOrchestratorHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var providerHealths = new List<ImageGenerationProviderHealth>();

        foreach (var provider in _providers)
        {
            try
            {
                var health = await provider.GetHealthAsync(cancellationToken);
                providerHealths.Add(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health for provider {ProviderId}", provider.ProviderId);
                providerHealths.Add(new ImageGenerationProviderHealth
                {
                    IsHealthy = false,
                    Status = $"Error: {ex.Message}",
                    ResponseTimeMs = 0,
                    AvailableModelsCount = 0,
                    LastChecked = DateTime.UtcNow
                });
            }
        }

        var healthyProviders = providerHealths.Count(h => h.IsHealthy);
        var totalProviders = _providers.Count;

        return new ImageGenerationOrchestratorHealth
        {
            IsHealthy = healthyProviders > 0,
            TotalProviders = totalProviders,
            HealthyProviders = healthyProviders,
            ProviderHealths = providerHealths,
            LastChecked = DateTime.UtcNow
        };
    }

    private async Task<IImageGenerationProvider?> SelectBestProviderAsync(ImageGenerationRequest request, CancellationToken cancellationToken)
    {
        // Filter available providers
        var availableProviders = _providers.Where(p => p.IsAvailable).ToList();
        if (!availableProviders.Any())
        {
            _logger.LogWarning("No available image generation providers found");
            return null;
        }

        // Apply configuration preferences
        var preferredProviders = availableProviders.Where(p => 
            _configuration.PreferredProviders.Contains(p.ProviderId)).ToList();

        if (preferredProviders.Any())
        {
            availableProviders = preferredProviders;
        }

        // Check online/offline requirements
        if (_configuration.PreferOffline)
        {
            var offlineProviders = availableProviders.Where(p => !p.RequiresOnline).ToList();
            if (offlineProviders.Any())
            {
                availableProviders = offlineProviders;
            }
        }

        // Select the first available provider (could be enhanced with more sophisticated selection logic)
        var selectedProvider = availableProviders.First();
        
        // Verify the provider is still healthy
        var health = await selectedProvider.GetHealthAsync(cancellationToken);
        if (!health.IsHealthy)
        {
            _logger.LogWarning("Selected provider {Provider} is not healthy, trying alternatives", selectedProvider.ProviderId);
            
            // Try other providers
            var alternativeProviders = availableProviders.Skip(1).ToList();
            foreach (var provider in alternativeProviders)
            {
                var altHealth = await provider.GetHealthAsync(cancellationToken);
                if (altHealth.IsHealthy)
                {
                    return provider;
                }
            }
            
            // If no healthy providers found, return the first one anyway
            _logger.LogWarning("No healthy providers found, using {Provider} despite health issues", selectedProvider.ProviderId);
        }

        return selectedProvider;
    }
}

/// <summary>
/// Configuration for image generation orchestration
/// </summary>
public record ImageGenerationConfiguration
{
    /// <summary>
    /// Preferred providers in order of preference
    /// </summary>
    public List<string> PreferredProviders { get; init; } = new();
    
    /// <summary>
    /// Whether to prefer offline providers
    /// </summary>
    public bool PreferOffline { get; init; } = true;
    
    /// <summary>
    /// Default image generation parameters
    /// </summary>
    public ImageGenerationRequest DefaultParameters { get; init; } = new();
    
    /// <summary>
    /// Maximum retry attempts for failed generations
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;
    
    /// <summary>
    /// Timeout for image generation requests
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Information about a provider
/// </summary>
public record ProviderInfo
{
    public string ProviderId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool RequiresOnline { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsHealthy { get; init; }
    public string HealthStatus { get; init; } = string.Empty;
    public List<ImageGenerationModel> AvailableModels { get; init; } = new();
    public DateTime LastHealthCheck { get; init; }
}

/// <summary>
/// Health status of the image generation orchestrator
/// </summary>
public record ImageGenerationOrchestratorHealth
{
    public bool IsHealthy { get; init; }
    public int TotalProviders { get; init; }
    public int HealthyProviders { get; init; }
    public List<ImageGenerationProviderHealth> ProviderHealths { get; init; } = new();
    public DateTime LastChecked { get; init; }
}
