using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Commands.Iteration;

/// <summary>
/// Pipeline command for optimizing existing iteration code.
/// This class acts as an orchestrator, delegating specific functionality to partial class implementations.
/// </summary>
[Command("iteration.optimize")]
public partial class OptimizeIterationCommand : ICommand<OptimizeIterationRequest, OptimizeIterationResponse>
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly ILogger<OptimizeIterationCommand> _logger;
    
    public OptimizeIterationCommand(
        IIterationStrategySelector strategySelector,
        ILogger<OptimizeIterationCommand> logger)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}