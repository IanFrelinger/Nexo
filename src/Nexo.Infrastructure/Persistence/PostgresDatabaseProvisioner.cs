using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Database;
using Nexo.Core.Application.Persistence.Ports;
using Npgsql;

namespace Nexo.Infrastructure.Persistence;

/// <summary>
/// Provisions isolated Postgres instances for tests and ephemeral workflows.
/// </summary>
public sealed class PostgresDatabaseProvisioner : IDatabaseProvisioner
{
    private readonly IEphemeralDatabaseLifecycle? _ephemeralLifecycle;
    private readonly ILogger<PostgresDatabaseProvisioner> _logger;

    public PostgresDatabaseProvisioner(
        ILogger<PostgresDatabaseProvisioner> logger,
        IEphemeralDatabaseLifecycle? ephemeralLifecycle = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ephemeralLifecycle = ephemeralLifecycle;
    }

    /// <inheritdoc />
    public async Task<IIsolatedDatabase> CreateAsync(
        DatabaseProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        return request.Isolation switch
        {
            DatabaseIsolationLevel.DedicatedContainer => await CreateDedicatedAsync(request, cancellationToken),
            DatabaseIsolationLevel.SharedServerNewDatabase => await CreateNewDatabaseAsync(request, cancellationToken),
            DatabaseIsolationLevel.SharedSchemaNamespaced => await CreateSchemaAsync(request, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Isolation, "Unsupported isolation level.")
        };
    }

    private async Task<IIsolatedDatabase> CreateDedicatedAsync(
        DatabaseProvisionRequest request,
        CancellationToken cancellationToken)
    {
        if (_ephemeralLifecycle == null)
        {
            throw new InvalidOperationException(
                "DedicatedContainer isolation requires IEphemeralDatabaseLifecycle (register PostgresEphemeralLifecycle or a test fake).");
        }

        var options = new EphemeralDbOptions(
            Engine: "postgres",
            ImageTag: request.ImageTag,
            Database: request.DatabaseName ?? "nexo",
            Password: request.Password);

        var started = await _ephemeralLifecycle.StartAsync(options, cancellationToken);
        _logger.LogInformation(
            "Provisioned dedicated Postgres container {ContainerId} on host port {Port}",
            started.ContainerId,
            started.HostPort);

        return new PostgresIsolatedDatabase(
            request.Isolation,
            started.ConnectionString,
            started.ContainerId,
            dropDatabaseName: null,
            dropSchemaName: null,
            adminConnectionString: null,
            _ephemeralLifecycle);
    }

    private static async Task<IIsolatedDatabase> CreateNewDatabaseAsync(
        DatabaseProvisionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdminConnectionString))
        {
            throw new ArgumentException(
                "AdminConnectionString is required for SharedServerNewDatabase isolation.",
                nameof(request));
        }

        var admin = new NpgsqlConnectionStringBuilder(request.AdminConnectionString);
        var dbName = SanitizeIdentifier(request.DatabaseName ?? "nexo_" + Guid.NewGuid().ToString("N")[..12]);

        await using (var conn = new NpgsqlConnection(request.AdminConnectionString))
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(
                $"CREATE DATABASE \"{dbName.Replace("\"", "\"\"")}\"",
                conn);
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        admin.Database = dbName;
        return new PostgresIsolatedDatabase(
            request.Isolation,
            admin.ConnectionString,
            backendResourceId: null,
            dropDatabaseName: dbName,
            dropSchemaName: null,
            adminConnectionString: request.AdminConnectionString,
            ephemeralLifecycle: null);
    }

    private static async Task<IIsolatedDatabase> CreateSchemaAsync(
        DatabaseProvisionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdminConnectionString))
        {
            throw new ArgumentException(
                "AdminConnectionString is required for SharedSchemaNamespaced isolation.",
                nameof(request));
        }

        var schema = SanitizeIdentifier(request.DatabaseName ?? "nexo_" + Guid.NewGuid().ToString("N")[..12]);

        await using (var conn = new NpgsqlConnection(request.AdminConnectionString))
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(
                $"CREATE SCHEMA IF NOT EXISTS \"{schema.Replace("\"", "\"\"")}\"",
                conn);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var app = new NpgsqlConnectionStringBuilder(request.AdminConnectionString)
        {
            SearchPath = schema
        };

        return new PostgresIsolatedDatabase(
            request.Isolation,
            app.ConnectionString,
            backendResourceId: null,
            dropDatabaseName: null,
            dropSchemaName: schema,
            adminConnectionString: request.AdminConnectionString,
            ephemeralLifecycle: null);
    }

    private static string SanitizeIdentifier(string value)
    {
        var s = value.Trim().ToLowerInvariant();
        if (s.Length == 0)
        {
            s = "nexo_" + Guid.NewGuid().ToString("N")[..8];
        }

        Span<char> buffer = stackalloc char[s.Length];
        var j = 0;
        foreach (var c in s)
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '_')
            {
                buffer[j++] = c;
            }
        }

        return j == 0 ? "nexo_" + Guid.NewGuid().ToString("N")[..8] : new string(buffer[..j]);
    }

    private sealed class PostgresIsolatedDatabase : IIsolatedDatabase
    {
        private readonly DatabaseIsolationLevel _isolation;
        private readonly string? _dropDatabaseName;
        private readonly string? _dropSchemaName;
        private readonly string? _adminConnectionString;
        private readonly IEphemeralDatabaseLifecycle? _ephemeralLifecycle;
        private int _disposed;

        public PostgresIsolatedDatabase(
            DatabaseIsolationLevel isolation,
            string connectionString,
            string? backendResourceId,
            string? dropDatabaseName,
            string? dropSchemaName,
            string? adminConnectionString,
            IEphemeralDatabaseLifecycle? ephemeralLifecycle)
        {
            _isolation = isolation;
            ConnectionString = connectionString;
            BackendResourceId = backendResourceId;
            _dropDatabaseName = dropDatabaseName;
            _dropSchemaName = dropSchemaName;
            _adminConnectionString = adminConnectionString;
            _ephemeralLifecycle = ephemeralLifecycle;
        }

        public string ConnectionString { get; }

        public DatabaseIsolationLevel Isolation => _isolation;

        public string? BackendResourceId { get; }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (_isolation == DatabaseIsolationLevel.DedicatedContainer &&
                _ephemeralLifecycle != null &&
                !string.IsNullOrEmpty(BackendResourceId))
            {
                await _ephemeralLifecycle.StopAsync(BackendResourceId!).ConfigureAwait(false);
                return;
            }

            if (_isolation == DatabaseIsolationLevel.SharedServerNewDatabase &&
                !string.IsNullOrEmpty(_dropDatabaseName) &&
                !string.IsNullOrEmpty(_adminConnectionString))
            {
                await using var conn = new NpgsqlConnection(_adminConnectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(
                    $"DROP DATABASE IF EXISTS \"{_dropDatabaseName.Replace("\"", "\"\"")}\" WITH (FORCE)",
                    conn);
                cmd.CommandTimeout = 120;
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return;
            }

            if (_isolation == DatabaseIsolationLevel.SharedSchemaNamespaced &&
                !string.IsNullOrEmpty(_dropSchemaName) &&
                !string.IsNullOrEmpty(_adminConnectionString))
            {
                await using var conn = new NpgsqlConnection(_adminConnectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(
                    $"DROP SCHEMA IF EXISTS \"{_dropSchemaName.Replace("\"", "\"\"")}\" CASCADE",
                    conn);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
