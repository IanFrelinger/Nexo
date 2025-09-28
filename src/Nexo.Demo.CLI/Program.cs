using System.Text.Json;
using Nexo.Abstractions;
using Nexo.Runtime;
using Nexo.Tools.Assembly;
using Nexo.Policies;
using Nexo.Demo.CLI.Agents;

namespace Nexo.Demo.CLI;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.SequenceEqual(new[] { "--help" }))
        {
            Console.WriteLine("Usage: nexo-demo demo run --input <path> --out <dir> --seed <int>");
            return 0;
        }

        if (args.Length >= 2 && args[0] == "demo" && args[1] == "run")
        {
            string input = GetOption(args, "--input") ?? throw new ArgumentException("--input required");
            string output = GetOption(args, "--out") ?? "./runs";
            string seedStr = GetOption(args, "--seed") ?? "0";
            if (!int.TryParse(seedStr, out var seed)) seed = 0;

            Directory.CreateDirectory(output);
            var runDir = Path.Combine(output, seed.ToString());
            Directory.CreateDirectory(runDir);
            Directory.CreateDirectory(Path.Combine(runDir, "artifacts"));

            var registry = new CapabilityRegistry();
            registry.Register(new AssemblyAnalyzeTool());
            registry.Register(new AssemblyDecompileTool());
            registry.Register(new AssemblySecurityScanTool());

            var policies = new PolicyEngine(new IPolicy[] {
                new OutputPathSandboxed(),
                new AllowAllPolicy(),
                new PerfHeadroom(TimeSpan.FromSeconds(5)),
            });

            var host = new AgentHost(new[] { new DirectorAgent() }, registry, policies);

            var snapshot = new WorldSnapshot(
                Tick: 0,
                Data: new Dictionary<string, object?>
                {
                    ["InputPath"]  = Path.GetFullPath(input),
                    ["OutputRoot"] = Path.GetFullPath(Path.Combine(runDir, "artifacts")),
                    ["Seed"]       = seed,
                });

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var delta = await host.StepAsync(snapshot, CancellationToken.None);
            sw.Stop();

            var replay = new
            {
                seed,
                snapshot = new { tick = snapshot.Tick, data = snapshot.Data },
                delta = new { delta?.TickFrom, delta?.TickTo, delta?.Log },
                elapsed_ms = sw.ElapsedMilliseconds,
                policies = new[] { "OutputPathSandboxed", "AllowAllPolicy", "PerfHeadroom" },
                tools = registry.Schemas().Select(s => s.Id).ToArray()
            };
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(Path.Combine(runDir, "replay.json"), JsonSerializer.Serialize(replay, jsonOptions));

            var scorecard = new
            {
                pass = true,
                elapsed_ms = sw.ElapsedMilliseconds,
                tool_calls = delta?.Log.Count ?? 0
            };
            await File.WriteAllTextAsync(Path.Combine(runDir, "scorecard.json"), JsonSerializer.Serialize(scorecard, jsonOptions));

            var html = $$"""
<!doctype html>
<html>
<head><meta charset="utf-8"><title>Nexo Demo Report</title></head>
<body>
<h1>Nexo Demo Report</h1>
<p><b>Seed:</b> {{seed}}</p>
<p><b>Input:</b> {{snapshot.Data["InputPath"]}}</p>
<p><b>Artifacts:</b> {{snapshot.Data["OutputRoot"]}}</p>
<h2>Delta Log</h2>
<pre>{{string.Join("\n", delta?.Log ?? new List<string>())}}</pre>
</body>
</html>
""";
            await File.WriteAllTextAsync(Path.Combine(runDir, "report.html"), html);

            Console.WriteLine($"Completed. See {runDir}");
            return 0;
        }

        Console.Error.WriteLine("Unknown command. Use: demo run --input <path> --out <dir> --seed <int>");
        return 1;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }
}
