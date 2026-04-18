using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// Black-box smoke for runtime-studio CLI surfaces: observations, objectives,
/// proposals, and mode. Spawns the real <c>Nexo.CLI.dll</c> via <see cref="CliRunner"/>
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
}
