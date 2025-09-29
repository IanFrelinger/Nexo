using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Runtime;
using Nexo.Tools.Dev;
using Nexo.Policies.Dev;
using Nexo.Agents.Dev;

static string? GetOpt(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
    return null;
}

if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  nexo-dev demo run --root <path> --mode <heal|extend> --out <dir> [--seed <int>] [--edit <relfile>] [--find <s>] [--replace <s>]");
    return 0;
}

if (args.Length >= 2 && args[0] == "demo" && args[1] == "run")
{
    var root = Path.GetFullPath(GetOpt(args, "--root") ?? ".");
    var modeStr = (GetOpt(args, "--mode") ?? "heal").ToLowerInvariant();
    var mode = modeStr == "extend" ? DevMode.Extend : DevMode.Heal;
    var outDir = Path.GetFullPath(GetOpt(args, "--out") ?? "./runs");
    var seed = int.TryParse(GetOpt(args, "--seed"), out var sVal) ? sVal : 0;

    var editPath = GetOpt(args, "--edit") ?? "tests/SampleTests.cs";
    var findText = GetOpt(args, "--find") ?? "Be(1)";
    var replText = GetOpt(args, "--replace") ?? "Be(2)";

    Directory.CreateDirectory(outDir);
    var runDir = Path.Combine(outDir, $"{mode}-{seed}");
    Directory.CreateDirectory(runDir);

    var tools = new CapabilityRegistry();
    tools.Register(new DotnetBuildTool());
    tools.Register(new DotnetTestTool());
    tools.Register(new RepoFsWriteTool());
    tools.Register(new RepoFsSearchReplaceTool());
    tools.Register(new RepoGitCommitTool());
    tools.Register(new DocsUpdateTool());

    var policies = new PolicyEngine(new IPolicy[] {
        new PathAllowlist(), new MaxWriteSize(300_000), new BuildMustPassBeforeCommit()
    });

    var host = new AgentHost(new[] { new DevDirectorAgent(mode) }, tools, policies);

    var snapshot = new WorldSnapshot(0, new Dictionary<string,object?> {
        ["RepoRoot"] = root,
        ["Seed"] = seed,
        ["EditPath"] = editPath,
        ["FindText"] = findText,
        ["ReplaceText"] = replText,
        ["LastBuildOk"] = (object?)false,
        ["LastTestsOk"] = (object?)false
    });

    var allLogs = new List<string>();
    var committed = false;

    for (int tick = 0; tick < 6; tick++)
    {
        var delta = await host.StepAsync(snapshot, CancellationToken.None);
        if (delta != null) allLogs.AddRange(delta.Log);

        // Derive signals for next tick from logs
        bool? buildOk = null, testOk = null;
        foreach (var line in delta?.Log ?? Array.Empty<string>())
        {
            if (line.StartsWith("build:exit=")) buildOk = line.EndsWith("=0");
            if (line.StartsWith("test:exit="))  testOk  = line.EndsWith("=0");
            if (line.StartsWith("commit:")) committed = true;
        }

        var nextData = new Dictionary<string, object?>(snapshot.Data);
        if (buildOk is not null) nextData["LastBuildOk"] = buildOk.Value;
        if (testOk  is not null) nextData["LastTestsOk"] = testOk.Value;

        snapshot = new WorldSnapshot(snapshot.Tick + 1, nextData);
        if (committed) break;
    }

    // Artifacts
    var replay = new {
        seed, mode = mode.ToString().ToLowerInvariant(),
        snapshot = new { repoRoot = root },
        logs = allLogs
    };
    await File.WriteAllTextAsync(Path.Combine(runDir, "replay.json"),
        JsonSerializer.Serialize(replay, new JsonSerializerOptions { WriteIndented = true }));

    var pass = committed;
    var scorecard = new { pass, ticks = snapshot.Tick, logs = allLogs.Count };
    await File.WriteAllTextAsync(Path.Combine(runDir, "scorecard.json"),
        JsonSerializer.Serialize(scorecard, new JsonSerializerOptions { WriteIndented = true }));

    var html = $@"<!doctype html><html><head><meta charset=""utf-8""><title>Nexo Dev Demo</title></head>
<body><h1>Nexo Dev Demo</h1>
<p><b>Mode:</b> {mode}</p>
<p><b>Root:</b> {root}</p>
<h2>Log</h2><pre>{string.Join("\n", allLogs)}</pre>
</body></html>";
    await File.WriteAllTextAsync(Path.Combine(runDir, "report.html"), html);

    Console.WriteLine($"Done → {runDir}");
    return 0;
}

Console.Error.WriteLine("Unknown command. Use --help.");
return 1;
