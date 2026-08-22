using MediatR;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Configuration.Models;
using Ashlar.Core.Application.Configuration.Ports;

namespace Ashlar.Core.Application.Configuration.UseCases.GetConfiguration;

/// <summary>
/// MediatR handler for getting configuration.
/// 
/// Responsibilities:
/// - Loads configuration from IConfigurationService
/// - Returns current AshlarConfiguration settings
/// - Logs configuration loading operations
/// 
/// Part of the Application layer's use case pattern, following CQRS principles.
/// </summary>
public class GetConfigurationHandler : IRequestHandler<GetConfigurationQuery, AshlarConfiguration>
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<GetConfigurationHandler> _logger;

    /// <summary>Creates a handler that loads configuration via <see cref="IConfigurationService"/>.</summary>
    /// <param name="configurationService">Service that loads persisted configuration.</param>
    /// <param name="logger">Logger for configuration operations.</param>
    public GetConfigurationHandler(
        IConfigurationService configurationService,
        ILogger<GetConfigurationHandler> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Handles the query by loading current configuration.</summary>
    /// <param name="request">Configuration query (no parameters).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Current <see cref="AshlarConfiguration"/> settings.</returns>
    public async Task<AshlarConfiguration> Handle(
        GetConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading configuration");
        return await _configurationService.LoadAsync(cancellationToken);
    }
}

