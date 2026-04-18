using System.Text.Json;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// Black-box smoke for runtime-studio CLI surfaces: observations, objectives,
/// proposals, mode, and a short <c>daemon</c> run. Spawns the real <c>Nexo.CLI.dll</c> via <see cref="CliRunner"/>
/// with isolated env paths (no unit-test references to command handlers).
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
[Trait("Category", "Smoke")]
[Trait("Category", "RuntimeStudio")]
public sealed class RuntimeStudioBlackBoxSmokeTests : E2ETestBase
{
    /// <summary>Per-invocation process budget: CLI may cold-build once; keep above E2E blame-hang.</summary>
    private static readonly TimeSpan CliTimeout = TimeSpan.FromSeconds(90);

    public RuntimeStudioBlackBoxSmokeTests() : base("nexo-runtime-studio-blackbox") { }

    private static IReadOnlyDictionary<string, string?> StudioEnv(string tempRoot)
    {
        Directory.CreateDirectory(tempRoot);
        return new Dictionary<string, string?>
        {
            ["NEXO_OBSERVATIONS_PATH"] = Path.Combine(tempRoot, "observations.jsonl"),
            ["NEXO_OBJECTIVES_ROOT"] = Path.Combine(tempRoot, "objectives"),
            ["NEXO_FORGE_ROOT"] = Path.Combine(tempRoot, "forge"),
            ["NEXO_AGENT_MODE_PATH"] = Path.Combine(tempRoot, "agent-mode.json"),
        };
    }

    [Fact(Timeout = 120_000)]
    public async Task Observations_empty_log_exits_zero()
    {
        var env = StudioEnv(Path.Combine(TempDir, "studio-a"));
        var (code, stdout, stderr) = await RunCliAsync("background-agent observations --tail 3", env, CliTimeout);
        Assert.Equal(0, code);
        Assert.True(
            stdout.Contains("No matching", StringComparison.OrdinalIgnoreCase)
            || stdout.Contains("Source:", StringComparison.OrdinalIgnoreCase),
            $"Expected empty observations message. stdout:\n{stdout}\nstderr:\n{stderr}");
    }

    [Fact(Timeout = 120_000)]
    public async Task Observations_invalid_kind_exits_nonzero()
    {
        var env = StudioEnv(Path.Combine(TempDir, "studio-b"));
        var (code, _, stderr) = await RunCliAsync("background-agent observations --kind NotARealKind", env, CliTimeout);
        Assert.Equal(2, code);
        Assert.Contains("Unknown", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Timeout = 120_000)]
    public async Task Objectives_add_list_block_unblock_stats_report_exit_zero()
    {
        var env = StudioEnv(Path.Combine(TempDir, "studio-c"));
        var id = "bb-smoke-" + Guid.NewGuid().ToString("N")[..8];

        var (addCode, _, addErr) = await RunCliAsync(
            $"background-agent objectives add --id {id} --title \"Smoke item\" --body \"verify cli\" --priority 5",
            env,
            CliTimeout);
        Assert.Equal(0, addCode);
        Assert.Equal(string.Empty, addErr);

        var (listCode, listOut, _) = await RunCliAsync("background-agent objectives list", env, CliTimeout);
        Assert.Equal(0, listCode);
        Assert.Contains(id, listOut, StringComparison.OrdinalIgnoreCase);

        var (blockCode, _, _) = await RunCliAsync($"background-agent objectives block {id} --reason \"smoke\"", env, CliTimeout);
        Assert.Equal(0, blockCode);

        var (unblockCode, _, _) = await RunCliAsync($"background-agent objectives unblock {id}", env, CliTimeout);
        Assert.Equal(0, unblockCode);

        var (statsCode, statsOut, _) = await RunCliAsync("background-agent objectives stats", env, CliTimeout);
        Assert.Equal(0, statsCode);
        Assert.Contains("Total objectives", statsOut, StringComparison.OrdinalIgnoreCase);

        var (reportCode, reportOut, _) = await RunCliAsync($"background-agent objectives report --id {id} --format-json", env, CliTimeout);
        Assert.Equal(0, reportCode);
        Assert.Contains("\"ok\": true", reportOut, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(id, reportOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Timeout = 120_000)]
    public async Task Proposals_list_stats_janitor_exit_zero_on_empty_queue()
    {
        var env = StudioEnv(Path.Combine(TempDir, "studio-d"));

        var (listCode, listOut, _) = await RunCliAsync("background-agent proposals list", env, CliTimeout);
        Assert.Equal(0, listCode);
        Assert.True(
            listOut.Contains("No matching", StringComparison.OrdinalIgnoreCase)
            || listOut.Contains("Proposals: 0", StringComparison.OrdinalIgnoreCase),
            listOut);

        var (statsCode, statsOut, _) = await RunCliAsync("background-agent proposals stats", env, CliTimeout);
        Assert.Equal(0, statsCode);
        Assert.Contains("Total proposals", statsOut, StringComparison.OrdinalIgnoreCase);

        var (janCode, janOut, _) = await RunCliAsync("background-agent proposals janitor --format-json", env, CliTimeout);
        Assert.Equal(0, janCode);
        Assert.Contains("\"ok\": true", janOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Timeout = 120_000)]
    public async Task Mode_get_and_set_round_trip()
    {
        var env = StudioEnv(Path.Combine(TempDir, "studio-e"));

        var (get0, out0, _) = await RunCliAsync("background-agent mode get", env, CliTimeout);
        Assert.Equal(0, get0);
        Assert.False(string.IsNullOrWhiteSpace(out0));

        var (setCode, _, setErr) = await RunCliAsync("background-agent mode set --value passive", env, CliTimeout);
        Assert.Equal(0, setCode);
        Assert.Equal(string.Empty, setErr);

        var (get1, out1, _) = await RunCliAsync("background-agent mode get", env, CliTimeout);
        Assert.Equal(0, get1);
        Assert.Contains("passive", out1, StringComparison.OrdinalIgnoreCase);

        // Restore active so subsequent tests on shared CI workers are not left in passive.
        var (restoreCode, _, _) = await RunCliAsync("background-agent mode set --value active", env, CliTimeout);
        Assert.Equal(0, restoreCode);
    }

    /// <summary>
    /// Phase 2 slice: prove the hosted background-agent process starts and shuts down cleanly
    /// (timed <c>--duration</c>) with a minimal agent config — no agent tick required.
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task Daemon_minimal_config_timed_run_exits_zero()
    {
        var root = Path.Combine(TempDir, "studio-daemon");
        Directory.CreateDirectory(root);
        var cfgPath = Path.Combine(root, "daemon-smoke.json");
        await File.WriteAllTextAsync(cfgPath, """
            {
              "BackgroundAgents": {
                "Agents": [
                  {
                    "Id": "daemon-smoke",
                    "Name": "Daemon Smoke",
                    "Role": "optimizer",
                    "ModelProvider": "deterministic",
                    "Commands": [ "analyze" ],
                    "Parameters": { "AnalysisPath": "." },
                    "Schedule": {
                      "Type": "Interval",
                      "Interval": "24:00:00",
                      "InitialDelay": "24:00:00"
                    },
                    "Enabled": true,
                    "MaxDataSensitivity": "Public"
                  }
                ]
              }
            }
            """);

        var env = StudioEnv(Path.Combine(root, "studio"));
        var merged = new Dictionary<string, string?>(env) { ["NEXO_ALLOW_MOCK"] = "1" };

        var daemonTimeout = TimeSpan.FromSeconds(150);
        var (code, _, stderr) = await RunCliAsync(
            $"background-agent daemon --config \"{cfgPath}\" --duration 5s --disable-observation",
            merged,
            daemonTimeout);
        Assert.Equal(0, code);
        Assert.Equal(string.Empty, stderr);
    }

    /// <summary>Daemon starts with observation pipeline enabled (heavier than <c>--disable-observation</c>).</summary>
    [Fact(Timeout = 300_000)]
    public async Task Daemon_minimal_config_timed_run_with_observation_pipeline_exits_zero()
    {
        var root = Path.Combine(TempDir, "studio-daemon-obs");
        Directory.CreateDirectory(root);
        var cfgPath = Path.Combine(root, "daemon-obs.json");
        await File.WriteAllTextAsync(cfgPath, """
            {
              "BackgroundAgents": {
                "Agents": [
                  {
                    "Id": "daemon-obs-smoke",
                    "Name": "Daemon Obs Smoke",
                    "Role": "optimizer",
                    "ModelProvider": "deterministic",
                    "Commands": [ "analyze" ],
                    "Parameters": { "AnalysisPath": "." },
                    "Schedule": {
                      "Type": "Interval",
                      "Interval": "24:00:00",
                      "InitialDelay": "24:00:00"
                    },
                    "Enabled": true,
                    "MaxDataSensitivity": "Public"
                  }
                ]
              }
            }
            """);

        var envRoot = Path.Combine(root, "studio");
        var env = StudioEnv(envRoot);
        var merged = new Dictionary<string, string?>(env) { ["NEXO_ALLOW_MOCK"] = "1" };

        var (code, _, stderr) = await RunCliAsync(
            $"background-agent daemon --config \"{cfgPath}\" --duration 5s",
            merged,
            TimeSpan.FromSeconds(150));
        Assert.Equal(0, code);
        Assert.Equal(string.Empty, stderr);
    }

    /// <summary>Operators can drop a JSON file into <c>forge/proposed</c> and list/show it via CLI.</summary>
    [Fact(Timeout = 120_000)]
    public async Task Proposals_disk_seed_lists_and_shows()
    {
        var root = Path.Combine(TempDir, "studio-proposals-seed");
        var env = StudioEnv(root);
        var forgeRoot = env["NEXO_FORGE_ROOT"]!;
        var proposedDir = Path.Combine(forgeRoot, "proposed");
        Directory.CreateDirectory(proposedDir);
        var id = "seed-" + Guid.NewGuid().ToString("N")[..10];
        var ts = DateTimeOffset.UtcNow;
        var payload = JsonSerializer.Serialize(new
        {
            Id = id,
            TargetPath = "src/Seeded.cs",
            NewContent = "// seeded",
            Summary = "disk seed",
            Status = "Proposed",
            CreatedAt = ts,
            UpdatedAt = ts
        });
        await File.WriteAllTextAsync(Path.Combine(proposedDir, id + ".json"), payload);

        var (listCode, listOut, listErr) = await RunCliAsync("background-agent proposals list", env, CliTimeout);
        Assert.Equal(0, listCode);
        Assert.Equal(string.Empty, listErr);
        Assert.Contains(id, listOut, StringComparison.OrdinalIgnoreCase);

        var (showCode, showOut, showErr) = await RunCliAsync($"background-agent proposals show {id}", env, CliTimeout);
        Assert.Equal(0, showCode);
        Assert.Equal(string.Empty, showErr);
        Assert.Contains(id, showOut, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src/Seeded.cs", showOut, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Phase 3: extender with no pinned <c>Objective</c> parameter claims the next pending
    /// backlog item during a timed daemon run, executes one self-extend cycle, then releases
    /// it back to pending with <c>Attempts</c> incremented (see <c>SelfExtendRunnerAdapter</c>).
    /// </summary>
    [Fact(Timeout = 300_000)]
    public async Task Daemon_extender_claims_objective_from_store_increments_attempts()
    {
        var root = Path.Combine(TempDir, "studio-daemon-claim");
        Directory.CreateDirectory(root);
        var sandboxRepo = Path.Combine(root, "sandbox-repo");
        Directory.CreateDirectory(sandboxRepo);

        var envRoot = Path.Combine(root, "studio");
        var env = StudioEnv(envRoot);
        var id = "claim-" + Guid.NewGuid().ToString("N")[..8];

        var (addCode, _, addErr) = await RunCliAsync(
            $"background-agent objectives add --id {id} --title \"Daemon claim smoke\" --body \"black-box\" --priority 1",
            env,
            CliTimeout);
        Assert.Equal(0, addCode);
        Assert.Equal(string.Empty, addErr);

        var cfgPath = Path.Combine(root, "daemon-claim.json");
        var cfgPayload = new
        {
            BackgroundAgents = new
            {
                Agents = new[]
                {
                    new
                    {
                        Id = "claim-smoke",
                        Name = "Objective Claim Smoke",
                        Role = "extender",
                        ModelProvider = "deterministic",
                        Commands = new[] { "self_extend" },
                        Parameters = new Dictionary<string, string>
                        {
                            ["RepoRoot"] = sandboxRepo,
                            ["AgentName"] = "claim-smoke-owner"
                        },
                        Schedule = new
                        {
                            Type = "Interval",
                            Interval = "01:00:00",
                            InitialDelay = "00:00:02"
                        },
                        Enabled = true,
                        MaxDataSensitivity = "Public"
                    }
                }
            }
        };
        await File.WriteAllTextAsync(cfgPath, JsonSerializer.Serialize(cfgPayload, new JsonSerializerOptions { WriteIndented = true }));

        var merged = new Dictionary<string, string?>(env) { ["NEXO_ALLOW_MOCK"] = "1" };
        var (daemonCode, _, daemonErr) = await RunCliAsync(
            $"background-agent daemon --config \"{cfgPath}\" --duration 20s --disable-observation",
            merged,
            TimeSpan.FromSeconds(120));
        Assert.Equal(0, daemonCode);
        Assert.Equal(string.Empty, daemonErr);

        var (showCode, showOut, showErr) = await RunCliAsync($"background-agent objectives show {id}", env, CliTimeout);
        Assert.Equal(0, showCode);
        Assert.Equal(string.Empty, showErr);
        Assert.Contains("Pending", showOut, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Attempts:   1", showOut, StringComparison.Ordinal);
    }
}
