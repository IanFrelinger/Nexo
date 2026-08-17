using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Mesh;

/// <summary>
/// Shared Docker Compose mesh lab for <see cref="MeshLabDockerE2ETests"/>.
/// Enable with <c>NEXO_RUN_MESH_LAB=1</c> (requires Docker, bash, python3).
/// </summary>
public sealed class MeshLabDockerFixture : IAsyncLifetime
{
    /// <summary>Serializes compose on one machine (dotnet test may run net8 + net10 hosts in parallel).</summary>
    private static readonly Mutex s_meshLabGate =
        new(initiallyOwned: false, name: "Nexo.MeshLab.DockerE2E.v1");

    private bool _ownsMeshLabGate;
    private string? _repoRoot;
    private string? _envFilePath;
    private string? _composeProjectName;
    private string? _skipReason;
    private string _peerAHost = "127.0.0.1:18081";
    private string _peerBHost = "127.0.0.1:18082";
    private string _workerHost = "127.0.0.1:18083";

    public bool IsReady { get; private set; }

    public string? SkipReason => _skipReason;

    public string EnvFilePath =>
        _envFilePath ?? throw new InvalidOperationException("Mesh lab fixture was not initialized.");

    public IReadOnlyDictionary<string, string?> VerifyScriptEnvironment => new Dictionary<string, string?>
    {
        ["COMPOSE_PROJECT_NAME"] = _composeProjectName,
        ["MESH_LAB_PEER_A_HOST"] = _peerAHost,
        ["MESH_LAB_PEER_B_HOST"] = _peerBHost,
    };

    public async Task InitializeAsync()
    {
        if (!MeshLabDockerEnv.IsEnabled)
        {
            _skipReason = "Set NEXO_RUN_MESH_LAB=1 with Docker, bash, and python3 available.";
            return;
        }

        if (!s_meshLabGate.WaitOne(TimeSpan.FromMinutes(45)))
        {
            _skipReason = "Timed out waiting for mesh lab gate (another NEXO_RUN_MESH_LAB test may be running).";
            return;
        }

        _ownsMeshLabGate = true;

        _repoRoot = TestPaths.FindRepoRoot();

        if (!await HasWorkingDockerAsync().ConfigureAwait(false))
        {
            _skipReason = "Docker daemon not reachable (start Docker Desktop or dockerd).";
            return;
        }

        if (!await HasPython3Async().ConfigureAwait(false))
        {
            _skipReason = "python3 is required for mesh-lab-verify scripts.";
            return;
        }

        var portBase = 25000 + Math.Abs(Guid.NewGuid().GetHashCode() % 15000);
        _peerAHost = $"127.0.0.1:{portBase}";
        _peerBHost = $"127.0.0.1:{portBase + 1}";
        _workerHost = $"127.0.0.1:{portBase + 2}";

        var key = $"mesh-lab-dotnet-{Guid.NewGuid():N}";
        key = key.Length <= 64 ? key : key[..64];
        var bearer = $"{key}-bearer";
        var basic = $"{key}-basic";

        _envFilePath = Path.Combine(Path.GetTempPath(), $"nexo-mesh-lab-{Guid.NewGuid():N}.env");
        await File.WriteAllTextAsync(
                _envFilePath,
                $"""
                 Nexo__Security__ApiKey={key}
                 Nexo__Security__PeerB__BearerToken={bearer}
                 Nexo__Security__Worker__BasicAuthPassword={basic}
                 MESH_LAB_PEER_A_PUBLISH={_peerAHost}
                 MESH_LAB_PEER_B_PUBLISH={_peerBHost}
                 MESH_LAB_WORKER_PUBLISH={_workerHost}
                 """)
            .ConfigureAwait(false);

        _composeProjectName = $"nexo_mesh_dotnet_{Guid.NewGuid():N}"[..32];

        var composeEnv = new Dictionary<string, string?>
        {
            ["COMPOSE_PROJECT_NAME"] = _composeProjectName,
            ["DOCKER_DEFAULT_PLATFORM"] = Environment.GetEnvironmentVariable("DOCKER_DEFAULT_PLATFORM") ?? "linux/amd64",
        };

        await MeshLabProcessRunner.AssertSuccessAsync(
                _repoRoot,
                "docker",
                $"compose --profile workers -f deploy/compose/docker-compose.mesh-lab.yml --env-file \"{_envFilePath}\" config -q",
                composeEnv,
                TimeSpan.FromMinutes(3))
            .ConfigureAwait(false);

        var upExit = await TryComposeUpAsync(composeEnv).ConfigureAwait(false);
        if (upExit != 0)
        {
            _skipReason = "docker compose up failed (ports may be in use — stop other mesh lab stacks).";
            return;
        }

        if (!await WaitForPeerHealthAsync(_peerAHost).ConfigureAwait(false)
            || !await WaitForPeerHealthAsync(_peerBHost).ConfigureAwait(false))
        {
            _skipReason = "Mesh lab peers did not become healthy after compose up.";
            return;
        }

        IsReady = true;
    }

    private static async Task<bool> WaitForPeerHealthAsync(string host)
    {
        for (var attempt = 0; attempt < 90; attempt++)
        {
            var (exitCode, _, _) = await MeshLabProcessRunner.RunAsync(
                    Directory.GetCurrentDirectory(),
                    "curl",
                    $"-fsS -m 3 http://{host}/health",
                    timeout: TimeSpan.FromSeconds(10))
                .ConfigureAwait(false);

            if (exitCode == 0)
                return true;

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<int> TryComposeUpAsync(IReadOnlyDictionary<string, string?> composeEnv)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                var portBase = 25000 + Math.Abs(Guid.NewGuid().GetHashCode() % 15000);
                _peerAHost = $"127.0.0.1:{portBase}";
                _peerBHost = $"127.0.0.1:{portBase + 1}";
                _workerHost = $"127.0.0.1:{portBase + 2}";
                await RewriteEnvPublishPortsAsync().ConfigureAwait(false);
            }

            var (exitCode, _, stderr) = await MeshLabProcessRunner.RunAsync(
                    _repoRoot!,
                    "docker",
                    $"compose --profile workers -f deploy/compose/docker-compose.mesh-lab.yml --env-file \"{_envFilePath}\" up -d --build",
                    composeEnv,
                    TimeSpan.FromMinutes(40))
                .ConfigureAwait(false);

            if (exitCode == 0)
                return 0;

            if (!stderr.Contains("port is already allocated", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"docker compose up failed: {stderr}");

            await MeshLabProcessRunner.RunAsync(
                    _repoRoot!,
                    "docker",
                    $"compose --profile workers -f deploy/compose/docker-compose.mesh-lab.yml --env-file \"{_envFilePath}\" down -v",
                    composeEnv,
                    TimeSpan.FromMinutes(3))
                .ConfigureAwait(false);
        }

        return 1;
    }

    private async Task RewriteEnvPublishPortsAsync()
    {
        if (_envFilePath is null)
            return;

        var lines = await File.ReadAllLinesAsync(_envFilePath).ConfigureAwait(false);
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("MESH_LAB_PEER_A_PUBLISH=", StringComparison.Ordinal))
                lines[i] = $"MESH_LAB_PEER_A_PUBLISH={_peerAHost}";
            else if (lines[i].StartsWith("MESH_LAB_PEER_B_PUBLISH=", StringComparison.Ordinal))
                lines[i] = $"MESH_LAB_PEER_B_PUBLISH={_peerBHost}";
            else if (lines[i].StartsWith("MESH_LAB_WORKER_PUBLISH=", StringComparison.Ordinal))
                lines[i] = $"MESH_LAB_WORKER_PUBLISH={_workerHost}";
        }

        await File.WriteAllLinesAsync(_envFilePath, lines).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_repoRoot is null || _envFilePath is null || _composeProjectName is null)
                return;

            var composeEnv = new Dictionary<string, string?>
            {
                ["COMPOSE_PROJECT_NAME"] = _composeProjectName,
            };

            try
            {
                await MeshLabProcessRunner.RunAsync(
                        _repoRoot,
                        "docker",
                        $"compose --profile workers -f deploy/compose/docker-compose.mesh-lab.yml --env-file \"{_envFilePath}\" down -v",
                        composeEnv,
                        TimeSpan.FromMinutes(5))
                    .ConfigureAwait(false);
            }
            catch
            {
                // Best-effort teardown for optional tests.
            }

            try
            {
                File.Delete(_envFilePath);
            }
            catch
            {
                // ignored
            }

            IsReady = false;
        }
        finally
        {
            if (_ownsMeshLabGate)
            {
                s_meshLabGate.ReleaseMutex();
                _ownsMeshLabGate = false;
            }
        }
    }

    private static async Task<bool> HasWorkingDockerAsync()
    {
        var (exitCode, _, _) = await MeshLabProcessRunner.RunAsync(
                Directory.GetCurrentDirectory(),
                "docker",
                "info",
                timeout: TimeSpan.FromSeconds(30))
            .ConfigureAwait(false);
        return exitCode == 0;
    }

    private static async Task<bool> HasPython3Async()
    {
        var (exitCode, _, _) = await MeshLabProcessRunner.RunAsync(
                Directory.GetCurrentDirectory(),
                "python3",
                "--version",
                timeout: TimeSpan.FromSeconds(10))
            .ConfigureAwait(false);
        return exitCode == 0;
    }
}
