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
/// <param name="PostgresReadinessTimeout">
/// Max time to wait for Postgres to accept connections after provision.
/// Null uses a default of 60 seconds. <see cref="TimeSpan.Zero"/> skips readiness polling (tests only).
/// </param>
/// <param name="PostProvisionSqlBatches">
/// Optional SQL batches executed on the application connection string after readiness (DDL/DML for schema seeding).
/// </param>
public sealed record DatabaseProvisionRequest(
    DatabaseIsolationLevel Isolation,
    string? DatabaseName = null,
    string? ImageTag = null,
    string? Password = null,
    string? AdminConnectionString = null,
    TimeSpan? PostgresReadinessTimeout = null,
    IReadOnlyList<string>? PostProvisionSqlBatches = null);
