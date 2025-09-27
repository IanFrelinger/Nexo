using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Enums;
using Nexo.Feature.API.Models;
using System.Collections.Concurrent;

namespace Nexo.Feature.API.Services;

/// <summary>
/// Rate limiting configuration management functionality
/// </summary>
public partial class RateLimiter
{
    /// <summary>
    /// Configures rate limiting rules
    /// </summary>
    public async Task<RateLimitConfigurationResult> ConfigureRateLimitingAsync(RateLimitConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrEmpty(configuration.Identifier))
            {
                return new RateLimitConfigurationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Identifier is required"
                };
            }

            var bucketKey = GetBucketKey(configuration.Identifier, configuration.Scope);
            
            // Store configuration
            _configurations.AddOrUpdate(bucketKey, configuration, (key, oldValue) => configuration);

            // Update or create bucket with new configuration
            var bucket = GetOrCreateBucket(bucketKey, configuration.Scope);
            bucket.UpdateConfiguration(configuration.MaxRequests, TimeSpan.FromSeconds(configuration.TimeWindowSeconds));

            _logger.LogInformation("Configured rate limiting for {Identifier} ({Scope}): {MaxRequests} requests per {TimeWindowSeconds}s", 
                configuration.Identifier, configuration.Scope, configuration.MaxRequests, configuration.TimeWindowSeconds);

            return new RateLimitConfigurationResult
            {
                IsSuccess = true,
                Identifier = configuration.Identifier,
                ConfiguredAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring rate limiting for {Identifier}", configuration.Identifier);
            
            return new RateLimitConfigurationResult
            {
                IsSuccess = false,
                Identifier = configuration.Identifier,
                ErrorMessage = ex.Message,
                ConfiguredAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets default configuration for a scope
    /// </summary>
    private RateLimitConfiguration GetDefaultConfiguration(RateLimitScope scope)
    {
        return scope switch
        {
            RateLimitScope.User => new RateLimitConfiguration
            {
                MaxRequests = 100,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.Service => new RateLimitConfiguration
            {
                MaxRequests = 1000,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.Global => new RateLimitConfiguration
            {
                MaxRequests = 10000,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.IPAddress => new RateLimitConfiguration
            {
                MaxRequests = 200,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.APIKey => new RateLimitConfiguration
            {
                MaxRequests = 500,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.Endpoint => new RateLimitConfiguration
            {
                MaxRequests = 300,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            _ => new RateLimitConfiguration
            {
                MaxRequests = 100,
                TimeWindowSeconds = 60,
                Scope = scope
            }
        };
    }
}
