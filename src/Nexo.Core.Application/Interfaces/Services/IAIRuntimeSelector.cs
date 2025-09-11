using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Application.Interfaces.AI;

namespace Nexo.Core.Application.Interfaces.Services
{
    public interface IAIRuntimeSelector
    {
        Task<IAIProvider> SelectOptimalProviderAsync(AIEngineType engineType, Dictionary<string, object> context);
        Task<IAIEngine> SelectOptimalEngineAsync(AIEngineType engineType, Dictionary<string, object> context);
        Task<List<AIEngineInfo>> GetAvailableEnginesAsync();
        Task<AIEngineInfo> GetEngineInfoAsync(AIEngineType engineType);
        Task<bool> IsEngineAvailableAsync(AIEngineType engineType);
        Task<IAIProvider> SelectOptimalProviderAsync(AIOperationContext context);
        Task<IAIEngine> SelectBestEngineAsync(AIOperationContext context);
        Task<List<IAIProvider>> GetAvailableProvidersAsync();
    }
}
