using System.Diagnostics;
using Npgsql;
using Xunit;

namespace Nexo.Tests.Orchestration.Database;

[CollectionDefinition("PostgresDocker", DisableParallelization = true)]
public sealed class PostgresDockerCollection : ICollectionFixture<PostgresDockerFixture>;

/// <summary>
/// Optional Postgres server for provisioner integration tests.
/// Uses <c>NEXO_TEST_POSTGRES_ADMIN</c> when set; otherwise starts a disposable Docker container.
/// </summary>
public sealed class PostgresDockerFixture : IAsyncLifetime
{
    private string? _containerName;
    private bool _ownsContainer;

    public bool IsReady { get; private set; }

    public string? SkipReason { get; private set; }

    public string AdminConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var fromEnv = Environment.GetEnvironmentVariable("NEXO_TEST_POSTGRES_ADMIN");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            AdminConnectionString = fromEnv.Trim();
            if (await CanConnectAsync(AdminConnectionString))
            {
                IsReady = true;
                return;
            }

            SkipReason = "NEXO_TEST_POSTGRES_ADMIN is set but not reachable.";
            return;
        }

        if (!await IsDockerAvailableAsync())
        {
            SkipReason = "Docker is not available.";
            return;
        }

        _containerName = "nexo-pg-cov-" + Guid.NewGuid().ToString("N")[..10];
        var start = await RunProcessAsync(
            "docker",
            $"run -d --rm --name {_containerName} -e POSTGRES_PASSWORD=postgres -p 127.0.0.1::5432 postgres:16-alpine");
        if (start.ExitCode != 0)
        {
            SkipReason = $"Failed to start Postgres container: {start.StdErr}";
            return;
        }

        _ownsContainer = true;

        var portResult = await RunProcessAsync("docker", $"port {_containerName} 5432/tcp");
        if (portResult.ExitCode != 0)
        {
            SkipReason = $"Failed to resolve Postgres port: {portResult.StdErr}";
            await StopContainerAsync();
            return;
        }

        var portLine = portResult.StdOut.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        var port = portLine[(portLine.LastIndexOf(':') + 1)..];
        AdminConnectionString =
            $"Host=127.0.0.1;Port={port};Username=postgres;Password=postgres;Database=postgres;Timeout=5";

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (await CanConnectAsync(AdminConnectionString))
            {
                IsReady = true;
                return;
            }

            await Task.Delay(500);
        }

        SkipReason = "Postgres container did not become ready in time.";
        await StopContainerAsync();
    }

    public async Task DisposeAsync()
    {
        if (_ownsContainer)
            await StopContainerAsync();
    }

    private async Task StopContainerAsync()
    {
        if (string.IsNullOrWhiteSpace(_containerName))
            return;

        await RunProcessAsync("docker", $"stop {_containerName}");
        _containerName = null;
        _ownsContainer = false;
    }

    private static async Task<bool> IsDockerAvailableAsync()
    {
        var result = await RunProcessAsync("docker", "info");
        return result.ExitCode == 0;
    }

    private static async Task<bool> CanConnectAsync(string connectionString)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(string file, string args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }
}
