using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// CLI commands for iteration analysis and optimization.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class IterationCommands
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IterationCommands> _logger;
    
    public IterationCommands(IServiceProvider serviceProvider, ILogger<IterationCommands> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    // This class acts as an orchestrator for various iteration command functionalities,
    // with specific categories defined in partial classes.
}