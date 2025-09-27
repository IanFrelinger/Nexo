using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Service for selecting the best AI runtime for operations.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AIRuntimeSelector : IAIRuntimeSelector
    {
        private readonly ILogger<AIRuntimeSelector> _logger;
        private readonly List<IAIProvider> _providers;
        private readonly Dictionary<AIProviderType, IAIProvider> _providerMap;

        public AIRuntimeSelector(ILogger<AIRuntimeSelector> logger, IEnumerable<IAIProvider> providers)
        {
            _logger = logger;
            _providers = providers.ToList();
            _providerMap = _providers.ToDictionary(p => p.ProviderType, p => p);
        }

    }

    /// <summary>
    /// Exception thrown when no AI provider is available
    /// </summary>
    public partial class NoAIProviderAvailableException : Exception
    {
        public NoAIProviderAvailableException(string message) : base(message) { }
        public NoAIProviderAvailableException(string message, Exception innerException) : base(message, innerException) { }
    }
}