using Ashlar.Core.Application.Configuration.Models;

namespace Ashlar.Core.Application.Configuration.Ports;

/// <summary>
/// Port for configuration services.
/// </summary>
public interface IConfigurationService
{
    /// <summary>Loads configuration from the configured file location.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The loaded configuration, or defaults when no file exists.</returns>
    Task<AshlarConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves configuration to the configured file location.</summary>
    /// <param name="configuration">Configuration to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(AshlarConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>Gets the default configuration without reading from disk.</summary>
    /// <returns>Default <see cref="AshlarConfiguration"/> values.</returns>
    AshlarConfiguration GetDefault();
}

