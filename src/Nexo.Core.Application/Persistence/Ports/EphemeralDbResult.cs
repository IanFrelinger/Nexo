namespace Nexo.Core.Application.Persistence.Ports;

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
