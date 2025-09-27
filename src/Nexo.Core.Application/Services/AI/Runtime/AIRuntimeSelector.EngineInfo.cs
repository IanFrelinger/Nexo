using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Engine information functionality for AI runtime selection.
    /// </summary>
    public partial class AIRuntimeSelector
    {
        /// <summary>
        /// Gets all available AI engines
        /// </summary>
        public async Task<List<AIEngineInfo>> GetAvailableEnginesAsync()
        {
            var engines = new List<AIEngineInfo>();
            var providers = await GetAvailableProvidersAsync();

            foreach (var provider in providers)
            {
                try
                {
                    var providerModels = await provider.GetAvailableModelsAsync();
                    var providerEngines = providerModels.Select(model => new AIEngineInfo
                    {
                        Id = model.Id,
                        Name = model.Name,
                        ProviderType = AIProviderType.Mock,
                        EngineType = AIEngineType.Mock,
                        IsAvailable = true,
                        Configuration = new Dictionary<string, object>()
                    }).ToList();
                    engines.AddRange(providerEngines);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get engines from provider: {ProviderType}", provider.ProviderType);
                }
            }

            return engines;
        }

        /// <summary>
        /// Gets information about a specific engine type
        /// </summary>
        public async Task<AIEngineInfo> GetEngineInfoAsync(AIEngineType engineType)
        {
            var engines = await GetAvailableEnginesAsync();
            return engines.FirstOrDefault(e => e.EngineType == engineType) ?? 
                   new AIEngineInfo { EngineType = engineType, Name = "Unknown", IsAvailable = false };
        }

        /// <summary>
        /// Checks if an engine type is available
        /// </summary>
        public async Task<bool> IsEngineAvailableAsync(AIEngineType engineType)
        {
            try
            {
                var engineInfo = await GetEngineInfoAsync(engineType);
                return engineInfo.IsAvailable;
            }
            catch
            {
                return false;
            }
        }
    }
}
