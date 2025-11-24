using MediatR;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Configuration.Models;
using Nexo.Core.Application.Configuration.Ports;

namespace Nexo.Core.Application.Configuration.UseCases.GetConfiguration;

/// <summary>
/// Handler for getting configuration.
/// </summary>
public class GetConfigurationHandler : IRequestHandler<GetConfigurationQuery, NexoConfiguration>
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<GetConfigurationHandler> _logger;

    public GetConfigurationHandler(
        IConfigurationService configurationService,
        ILogger<GetConfigurationHandler> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NexoConfiguration> Handle(
        GetConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading configuration");
        return await _configurationService.LoadAsync(cancellationToken);
    }
}

