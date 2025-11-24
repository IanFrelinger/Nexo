using Nexo.Core.Application.Configuration.Models;

namespace Nexo.Core.Application.Configuration.Ports;

/// <summary>
/// Port for configuration services.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads configuration from file.
    /// </summary>
    Task<NexoConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves configuration to file.
    /// </summary>
    Task SaveAsync(NexoConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    NexoConfiguration GetDefault();
}

