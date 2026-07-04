namespace Nexo.Core.Application.Persistence.Ports;

/// <summary>
/// Port for ephemeral database lifecycle. Starts/stops database containers per session.
/// When NEXO_EPHEMERAL_DB=postgres (or mysql, redis), use this for integration tests or workflows.
/// </summary>
public interface IEphemeralDatabaseLifecycle
{
    /// <summary>
    /// Starts an ephemeral database container. Returns connection string and container ID.
    /// </summary>
    /// <param name="options">Database options (engine, image, password, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection string and container ID.</returns>
    Task<EphemeralDbResult> StartAsync(EphemeralDbOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops and removes the ephemeral database container.
    /// </summary>
    /// <param name="containerId">Container ID from StartAsync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(string containerId, CancellationToken cancellationToken = default);
}
