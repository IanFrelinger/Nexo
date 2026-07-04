using Nexo.Core.Application.Configuration.Models;

namespace Nexo.Core.Application.Configuration.Ports;

/// <summary>
/// Port for configuration services.
/// </summary>
public interface IConfigurationService
{
    /// <summary>Loads configuration from the configured file location.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The loaded configuration, or defaults when no file exists.</returns>
    Task<NexoConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves configuration to the configured file location.</summary>
    /// <param name="configuration">Configuration to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(NexoConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>Gets the default configuration without reading from disk.</summary>
    /// <returns>Default <see cref="NexoConfiguration"/> values.</returns>
    NexoConfiguration GetDefault();
}

