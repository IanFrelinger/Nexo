using System.Text.Json;
using GameDirector.Mcp;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Playtesting;

namespace GameDirector.Mcp.Tools;

/// <summary>Runs the standard Nexo Unity playtest adapter through MCP.</summary>
public sealed class RunBrPlaytestTool : IMcpTool
{
    private readonly IPlaytestRunRunner _runner;

    public RunBrPlaytestTool(IPlaytestRunRunner runner)
    {
        _runner = runner;
    }

    public string Name => "br_run_playtest";
    public string Description =>
        "Run one local Unity Weapon Lab first-pass playtest and return its report/evidence.";
    public JsonElement InputSchema => McpToolHelpers.Schema(
        new
        {
            build_path = new
            {
                type = "string",
                description = "Allowlisted Weapon Lab .app path"
            }
        },
        new
        {
            artifact_root = new
            {
                type = "string",
                description = "Local report/screenshot output root"
            }
        });

    public async Task<JsonElement> ExecuteAsync(
        JsonElement arguments,
        CancellationToken ct)
    {
        var buildPath =
            arguments.TryGetProperty("build_path", out var build)
                ? build.GetString()
                : null;
        var artifactRoot =
            arguments.TryGetProperty("artifact_root", out var artifacts)
                ? artifacts.GetString()
                : null;
        var config = new BackgroundAgentConfig
        {
            Id = "mcp-br-playtester",
            Name = "MCP BR Playtester",
            Role = "playtest",
            Commands = ["playtest"],
            Parameters = new Dictionary<string, object>
            {
                ["BuildPath"] =
                    buildPath ??
                    "../NexoBattleRoyale/Builds/WeaponLab/NexoBRWeaponLab.app",
                ["ArtifactRoot"] =
                    artifactRoot ??
                    ".nexo/playtest/br-weapon-lab"
            }
        };
        var result = await _runner.RunAsync(config, ct)
            .ConfigureAwait(false);
        return JsonSerializer.SerializeToElement(new
        {
            success = result.Success,
            summary = result.Summary,
            report_path = result.ReportPath,
            actions_executed = result.ActionsExecuted,
            facts = result.Facts
        });
    }
}

/// <summary>Returns the latest local BR playtest report.</summary>
public sealed class GetBrPlaytestReportTool : IMcpTool
{
    public string Name => "br_get_playtest_report";
    public string Description =>
        "Read the latest local BR Weapon Lab playtest report.";
    public JsonElement InputSchema => McpToolHelpers.Schema();

    public async Task<JsonElement> ExecuteAsync(
        JsonElement arguments,
        CancellationToken ct)
    {
        var root = Path.GetFullPath(
            Environment.GetEnvironmentVariable(
                "NEXO_BR_PLAYTEST_ARTIFACT_ROOT")
            ?? Path.Combine(
                Directory.GetCurrentDirectory(),
                ".nexo",
                "playtest",
                "br-weapon-lab"));
        var candidates = new[]
        {
            Path.Combine(root, "standard-background-agent-report.json"),
            Path.Combine(root, "report.json")
        };
        var path = candidates
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (path is null)
        {
            return JsonSerializer.SerializeToElement(new
            {
                found = false,
                root
            });
        }

        var raw = await File.ReadAllTextAsync(path, ct)
            .ConfigureAwait(false);
        using var document = JsonDocument.Parse(raw);
        return JsonSerializer.SerializeToElement(new
        {
            found = true,
            path,
            report = document.RootElement.Clone()
        });
    }
}
