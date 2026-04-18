using System.Text.Json;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.CLI;

/// <summary>
/// Playground: drives the real <b>Release</b>-built CLI against isolated runtime-studio
/// paths with a broad argument matrix and checks exit codes plus output invariants.
/// Opt-in via <c>Category=Playground</c> (not part of default smoke).
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
[Trait("Category", "Playground")]
[Trait("Category", "RuntimeStudio")]
public sealed class RuntimeStudioBlackBoxPlaygroundTests : E2ETestBase
{
    private const string ProdConfiguration = "Release";
    private static readonly TimeSpan CliTimeout = TimeSpan.FromSeconds(90);

    public RuntimeStudioBlackBoxPlaygroundTests() : base("nexo-runtime-studio-bb-play") { }

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

    private Task<(int Code, string Out, string Err)> Cli(
        string args,
        IReadOnlyDictionary<string, string?> env) =>
        RunCliAsync(args, env, CliTimeout, default, ProdConfiguration);

    [Fact(Timeout = 600_000)]
    public async Task Prod_cli_observations_matrix_exit_codes_and_output_shape()
    {
        var env = StudioEnv(Path.Combine(TempDir, "pg-obs"));
        var kinds = new[] { "Build", "build", "Test", "Analysis", "AgentAction", "UserSignal" };
        foreach (var k in kinds)
        {
            var (code, stdout, stderr) = await Cli($"background-agent observations --kind {k} --tail 5", env);
            Assert.Equal(0, code);
            Assert.True(
                stdout.Contains("No matching", StringComparison.OrdinalIgnoreCase)
                || stdout.Contains("Source:", StringComparison.OrdinalIgnoreCase),
                $"kind={k}\nstdout:\n{stdout}\nstderr:\n{stderr}");
            Assert.Equal(string.Empty, stderr);
        }

        foreach (var tail in new[] { 1, 3, 50 })
        {
            var (code, stdout, _) = await Cli($"background-agent observations --tail {tail}", env);
            Assert.Equal(0, code);
            Assert.Contains("Source:", stdout, StringComparison.OrdinalIgnoreCase);
        }

        var (sumCode, sumOut, _) = await Cli("background-agent observations --summary", env);
        Assert.Equal(0, sumCode);
        Assert.Contains("Source:", sumOut, StringComparison.OrdinalIgnoreCase);

        var (jsonCode, jsonOut, _) = await Cli("background-agent observations --tail 2 --format-json", env);
        Assert.Equal(0, jsonCode);
        using var doc = JsonDocument.Parse(jsonOut.Trim());
        Assert.True(doc.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean());

        foreach (var bad in new[] { "Nope", "not_a_real_kind", "%%%invalid%%%" })
        {
            var (c, _, err) = await Cli($"background-agent observations --kind {bad}", env);
            Assert.Equal(2, c);
            Assert.Contains("Unknown", err, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(Timeout = 600_000)]
    public async Task Prod_cli_objectives_matrix_lifecycle_and_validation()
    {
        var env = StudioEnv(Path.Combine(TempDir, "pg-obj"));
        var ids = new List<string>();
        foreach (var pri in new[] { 1, 10, 99, 1000 })
        {
            // List output truncates ids (~31 cols); keep ids short so plain-text list Contains() is reliable.
            var id = $"{pri}x{Guid.NewGuid():N}"[..18];
            ids.Add(id);
            var title = $"t-{pri}-{new string('x', Math.Min(40, pri))}";
            var body = $"body line for pri={pri}; chars: abc ABC123";
            var (addCode, _, addErr) = await Cli(
                $"background-agent objectives add --id {id} --title \"{title}\" --body \"{body}\" --priority {pri} --tag a --tag b",
                env);
            Assert.Equal(0, addCode);
            Assert.Equal(string.Empty, addErr);
        }

        var (listCode, listOut, _) = await Cli("background-agent objectives list", env);
        Assert.Equal(0, listCode);
        foreach (var id in ids)
            Assert.Contains(id, listOut, StringComparison.OrdinalIgnoreCase);

        foreach (var st in new[] { "Pending", "pending", "Done", "Blocked", "InProgress" })
        {
            var (c, o, _) = await Cli($"background-agent objectives list --status {st}", env);
            Assert.Equal(0, c);
            Assert.True(o.Contains("Source:", StringComparison.OrdinalIgnoreCase) || o.Contains("No matching", StringComparison.OrdinalIgnoreCase), o);
        }

        var (badList, _, badErr) = await Cli("background-agent objectives list --status NotARealStatus", env);
        Assert.Equal(2, badList);
        Assert.Contains("Unknown", badErr, StringComparison.OrdinalIgnoreCase);

        var target = ids[0];
        var (blk, _, _) = await Cli($"background-agent objectives block {target} --reason \"playground\"", env);
        Assert.Equal(0, blk);
        var (unblk, _, _) = await Cli($"background-agent objectives unblock {target}", env);
        Assert.Equal(0, unblk);

        var (statsCode, statsOut, _) = await Cli("background-agent objectives stats", env);
        Assert.Equal(0, statsCode);
        Assert.Contains("Total objectives", statsOut, StringComparison.OrdinalIgnoreCase);

        var (repCode, repOut, _) = await Cli($"background-agent objectives report --format-json", env);
        Assert.Equal(0, repCode);
        Assert.Contains("\"ok\": true", repOut, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ids[0], repOut, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Timeout = 600_000)]
    public async Task Prod_cli_proposals_matrix_and_mode_round_trip()
    {
        var env = StudioEnv(Path.Combine(TempDir, "pg-prop-mode"));

        var (listCode, listOut, _) = await Cli("background-agent proposals list", env);
        Assert.Equal(0, listCode);
        Assert.True(
            listOut.Contains("No matching", StringComparison.OrdinalIgnoreCase)
            || listOut.Contains("Proposals:", StringComparison.OrdinalIgnoreCase),
            listOut);

        var (listJsonCode, listJson, _) = await Cli("background-agent proposals list --format-json", env);
        Assert.Equal(0, listJsonCode);
        using (var d = JsonDocument.Parse(listJson.Trim()))
        {
            Assert.True(d.RootElement.TryGetProperty("ok", out var ok) && ok.GetBoolean());
            Assert.True(d.RootElement.TryGetProperty("count", out var count) && count.GetInt32() >= 0);
        }

        var (bad, _, err) = await Cli("background-agent proposals list --status NotAStatus", env);
        Assert.Equal(2, bad);
        Assert.Contains("Unknown", err, StringComparison.OrdinalIgnoreCase);

        var (statsCode, statsOut, _) = await Cli("background-agent proposals stats", env);
        Assert.Equal(0, statsCode);
        Assert.Contains("Total proposals", statsOut, StringComparison.OrdinalIgnoreCase);

        var (janCode, janOut, _) = await Cli("background-agent proposals janitor --format-json", env);
        Assert.Equal(0, janCode);
        Assert.Contains("\"ok\": true", janOut, StringComparison.OrdinalIgnoreCase);

        foreach (var mode in new[] { "passive", "semi-active", "active", "ambient" })
        {
            var (setCode, setOut, setErr) = await Cli($"background-agent mode set --value {mode}", env);
            Assert.Equal(0, setCode);
            Assert.Equal(string.Empty, setErr);
            Assert.Contains("set to", setOut, StringComparison.OrdinalIgnoreCase);

            var (getCode, getOut, _) = await Cli("background-agent mode get", env);
            Assert.Equal(0, getCode);
            Assert.Contains(mode, getOut, StringComparison.OrdinalIgnoreCase);
        }

        var (badMode, _, modeErr) = await Cli("background-agent mode set --value typo-mode", env);
        Assert.Equal(1, badMode);
        Assert.Contains("Unknown", modeErr, StringComparison.OrdinalIgnoreCase);

        var (restore, _, _) = await Cli("background-agent mode set --value active", env);
        Assert.Equal(0, restore);
    }
}
