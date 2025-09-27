using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Agents;

/// <summary>
/// AI agent specialized in platform-specific iteration optimizations.
/// This class acts as an orchestrator, delegating specific functionality to partial class implementations.
/// </summary>
public partial class PlatformIterationAgent : IAISpecializedAgent
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly IModelOrchestrator _modelOrchestrator;
    private readonly ILogger<PlatformIterationAgent> _logger;
    
    public string AgentId => "PlatformIteration";
    public AgentCapabilities Capabilities => AgentCapabilities.CodeGeneration | AgentCapabilities.PlatformOptimization;
    
    public PlatformIterationAgent(
        IIterationStrategySelector strategySelector,
        IModelOrchestrator modelOrchestrator,
        ILogger<PlatformIterationAgent> logger)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}