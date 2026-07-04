namespace Nexo.Abstractions.Database;

/// <summary>
/// A provisioned database instance with an explicit async lifecycle.
/// </summary>
public interface IIsolatedDatabase : IAsyncDisposable
{
    /// <summary>
    /// Connection string for application use (may be scoped to a schema when using shared-schema isolation).
    /// </summary>
    string ConnectionString { get; }

    /// <summary>Isolation tier describing how this database instance is separated from peers.</summary>
    DatabaseIsolationLevel Isolation { get; }

    /// <summary>
    /// Optional opaque backend id (e.g. Docker container id for ephemeral Postgres).
    /// </summary>
    string? BackendResourceId { get; }
}
