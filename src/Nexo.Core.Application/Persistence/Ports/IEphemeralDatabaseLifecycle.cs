namespace Nexo.Core.Application.Persistence.Ports;

/// <summary>
/// Options for starting an ephemeral database container.
/// </summary>
/// <param name="Engine">Database engine: postgres, mysql, redis.</param>
/// <param name="ImageTag">Optional image tag override (e.g. postgres:16-alpine).</param>
/// <param name="Database">Database name (for SQL engines).</param>
/// <param name="Password">Password for the root/admin user.</param>
/// <param name="InitScriptPath">Optional path to SQL init script to run on startup.</param>
public sealed record EphemeralDbOptions(
    string Engine,
    string? ImageTag = null,
    string? Database = null,
    string? Password = null,
    string? InitScriptPath = null);

/// <summary>
/// Result of starting an ephemeral database container.
/// </summary>
/// <param name="ConnectionString">Connection string for the database.</param>
/// <param name="ContainerId">Docker container ID. Pass to StopAsync to tear down.</param>
/// <param name="HostPort">The host port the database is listening on.</param>
public sealed record EphemeralDbResult(
    string ConnectionString,
    string ContainerId,
    int HostPort);

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
