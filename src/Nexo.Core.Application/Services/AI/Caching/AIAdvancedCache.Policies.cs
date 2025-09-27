using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Cache policy management for AIAdvancedCache.
    /// </summary>
    public partial class AIAdvancedCache
    {
        /// <summary>
        /// Creates a cache policy
        /// </summary>
        public Task<bool> CreatePolicyAsync(string policyName, CachePolicy policy)
        {
            try
            {
                _logger.LogInformation("Creating cache policy {PolicyName}", policyName);

                lock (_lockObject)
                {
                    _policies[policyName] = policy;
                }

                _logger.LogInformation("Cache policy {PolicyName} created successfully", policyName);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cache policy {PolicyName}", policyName);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets cache policy
        /// </summary>
        public Task<CachePolicy?> GetPolicyAsync(string policyName)
        {
            try
            {
                lock (_lockObject)
                {
                    _policies.TryGetValue(policyName, out var policy);
                    return Task.FromResult(policy);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get cache policy {PolicyName}", policyName);
                return Task.FromResult<CachePolicy?>(null);
            }
        }

        /// <summary>
        /// Gets cache policy (internal method)
        /// </summary>
        private CachePolicy GetPolicy(string? policyName)
        {
            if (string.IsNullOrEmpty(policyName))
                policyName = "default";

            lock (_lockObject)
            {
                if (_policies.TryGetValue(policyName, out var policy))
                    return policy;
            }

            // Return default policy
            return new CachePolicy
            {
                Name = "default",
                ExpirationTime = TimeSpan.FromHours(1),
                MaxSize = 1000,
                EvictionStrategy = EvictionStrategy.LRU
            };
        }
    }
}
