namespace Nexo.Abstractions.Database;

/// <summary>
/// Describes how strongly a database instance is isolated from other workloads.
/// </summary>
public enum DatabaseIsolationLevel
{
    /// <summary>
    /// Dedicated container or equivalent process boundary (strong isolation).
    /// </summary>
    DedicatedContainer,

    /// <summary>
    /// New logical database on an existing server (shared process, separate catalog).
    /// </summary>
    SharedServerNewDatabase,

    /// <summary>
    /// New schema (or namespace) within a shared database.
    /// </summary>
    SharedSchemaNamespaced
}
