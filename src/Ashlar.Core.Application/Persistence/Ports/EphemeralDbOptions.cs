namespace Ashlar.Core.Application.Persistence.Ports;

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
