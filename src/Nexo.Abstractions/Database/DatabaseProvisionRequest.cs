namespace Nexo.Abstractions.Database;

/// <summary>
/// Parameters for provisioning an isolated database instance.
/// </summary>
/// <param name="Isolation">How strongly the instance is isolated.</param>
/// <param name="DatabaseName">Logical database or schema name hint (engine-specific).</param>
/// <param name="ImageTag">Optional container image tag for dedicated-container isolation.</param>
/// <param name="Password">Optional password override.</param>
/// <param name="AdminConnectionString">
/// Admin connection string to the Postgres server (required for shared-server and shared-schema isolation).
/// </param>
public sealed record DatabaseProvisionRequest(
    DatabaseIsolationLevel Isolation,
    string? DatabaseName = null,
    string? ImageTag = null,
    string? Password = null,
    string? AdminConnectionString = null);
