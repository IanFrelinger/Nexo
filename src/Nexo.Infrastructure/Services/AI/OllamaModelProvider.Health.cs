using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Health and status functionality
    /// </summary>
    public partial class OllamaModelProvider
    {
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var response = await _httpClient.GetAsync("api/tags", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for Ollama provider");
                return false;
            }
        }

        public async Task<ProviderStatus> GetStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var isHealthy = await IsHealthyAsync(cancellationToken);
                var models = await GetAvailableModelsAsync(cancellationToken);

                return new ProviderStatus
                {
                    ProviderId = ProviderId,
                    IsHealthy = isHealthy,
                    AvailableModels = models.Count(),
                    LastChecked = DateTime.UtcNow,
                    Status = isHealthy ? "Healthy" : "Unhealthy"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Ollama provider status");
                return new ProviderStatus
                {
                    ProviderId = ProviderId,
                    IsHealthy = false,
                    AvailableModels = 0,
                    LastChecked = DateTime.UtcNow,
                    Status = "Error",
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<Dictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var status = await GetStatusAsync(cancellationToken);
                var models = await GetAvailableModelsAsync(cancellationToken);

                return new Dictionary<string, object>
                {
                    { "provider_id", ProviderId },
                    { "is_healthy", status.IsHealthy },
                    { "available_models", models.Count() },
                    { "last_checked", status.LastChecked },
                    { "status", status.Status },
                    { "base_url", _httpClient.BaseAddress?.ToString() ?? "unknown" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Ollama provider metrics");
                return new Dictionary<string, object>
                {
                    { "provider_id", ProviderId },
                    { "is_healthy", false },
                    { "error", ex.Message },
                    { "last_checked", DateTime.UtcNow }
                };
            }
        }
    }
}
