using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Database;
using Ashlar.Core.Application.Persistence.Ports;
using Npgsql;

namespace Ashlar.Infrastructure.Persistence;

/// <summary>
/// Provisions isolated Postgres instances for tests and ephemeral workflows.
/// After provision: optional readiness polling, optional post-provision SQL, then <see cref="IDatabaseProvisioningHook"/> in registration order.
/// </summary>
public sealed class PostgresDatabaseProvisioner : IDatabaseProvisioner
{
    private static readonly TimeSpan DefaultReadinessTimeout = TimeSpan.FromSeconds(60);

    private readonly IEphemeralDatabaseLifecycle? _ephemeralLifecycle;
    private readonly IReadOnlyList<IDatabaseProvisioningHook> _hooks;
    private readonly ILogger<PostgresDatabaseProvisioner> _logger;

    /// <summary>Initializes a new postgres database provisioner.</summary>
    public PostgresDatabaseProvisioner(
        ILogger<PostgresDatabaseProvisioner> logger,
        IEphemeralDatabaseLifecycle? ephemeralLifecycle = null,
        IEnumerable<IDatabaseProvisioningHook>? provisioningHooks = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ephemeralLifecycle = ephemeralLifecycle;
        _hooks = provisioningHooks?.ToArray() ?? Array.Empty<IDatabaseProvisioningHook>();
    }

    /// <inheritdoc />
    public async Task<IIsolatedDatabase> CreateAsync(
        DatabaseProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var db = request.Isolation switch
        {
            DatabaseIsolationLevel.DedicatedContainer => await CreateDedicatedAsync(request, cancellationToken),
            DatabaseIsolationLevel.SharedServerNewDatabase => await CreateNewDatabaseAsync(request, cancellationToken),
            DatabaseIsolationLevel.SharedSchemaNamespaced => await CreateSchemaAsync(request, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Isolation, "Unsupported isolation level.")
        };

        try
        {
            var readiness = request.PostgresReadinessTimeout ?? DefaultReadinessTimeout;
            if (readiness > TimeSpan.Zero)
            {
                await WaitForPostgresReadyAsync(db.ConnectionString, readiness, cancellationToken).ConfigureAwait(false);
            }

            if (request.PostProvisionSqlBatches is { Count: > 0 })
            {
                await RunPostProvisionSqlAsync(db.ConnectionString, request.PostProvisionSqlBatches, cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var hook in _hooks)
            {
                await hook.AfterProvisionAsync(db, request, cancellationToken).ConfigureAwait(false);
            }

            return db;
        }
        catch
        {
            try
            {
                await db.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose database after provisioning error");
            }

            throw;
        }
    }

    private static async Task WaitForPostgresReadyAsync(
        string connectionString,
        TimeSpan overallTimeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + overallTimeout;
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand("SELECT 1", conn) { CommandTimeout = 30 };
                await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                last = ex;
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException(
            $"Postgres did not become ready within {overallTimeout.TotalSeconds:0}s.",
            last);
    }

    private static async Task RunPostProvisionSqlAsync(
        string connectionString,
        IReadOnlyList<string> batches,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var cmd = new NpgsqlCommand(batch, conn) { CommandTimeout = 120 };
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
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
            Database: request.DatabaseName ?? "ashlar",
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
        var dbName = SanitizeIdentifier(request.DatabaseName ?? "ashlar_" + Guid.NewGuid().ToString("N")[..12]);

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

        var schema = SanitizeIdentifier(request.DatabaseName ?? "ashlar_" + Guid.NewGuid().ToString("N")[..12]);

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
            s = "ashlar_" + Guid.NewGuid().ToString("N")[..8];
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

        return j == 0 ? "ashlar_" + Guid.NewGuid().ToString("N")[..8] : new string(buffer[..j]);
    }

    private sealed class PostgresIsolatedDatabase : IIsolatedDatabase
    {
        private readonly DatabaseIsolationLevel _isolation;
        private readonly string? _dropDatabaseName;
        private readonly string? _dropSchemaName;
        private readonly string? _adminConnectionString;
        private readonly IEphemeralDatabaseLifecycle? _ephemeralLifecycle;
        private int _disposed;

        /// <summary>Initializes a new postgres isolated database.</summary>
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

        /// <summary>Connection string.</summary>
        public string ConnectionString { get; }

        /// <summary>Isolation.</summary>
        public DatabaseIsolationLevel Isolation => _isolation;

        /// <summary>Backend resource id.</summary>
        public string? BackendResourceId { get; }

        /// <summary>Dispose asynchronously.</summary>
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
