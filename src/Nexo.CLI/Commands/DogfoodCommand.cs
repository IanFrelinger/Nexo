using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Core.Application.Paths;
using Nexo.Infrastructure.Observation;

namespace Nexo.CLI.Commands;

/// <summary>
/// Dogfood validation commands — verify Nexo uses its own capabilities on itself.
/// North Star: Each block must pass its dogfood gate before moving on.
/// </summary>
public sealed class DogfoodCommand : Command
{
    public DogfoodCommand() : base("dogfood", "Dogfood validation: verify Nexo observes/adapts itself (North Star gates)")
    {
        var jsonOpt = new Option<bool>("--format-json", () => false, "Emit JSON output");

        var block1Cmd = new Command("block1", "Block 1 dogfood gate: verify observation pipeline watches Nexo's own dev workflow and stores patterns");
        block1Cmd.AddOption(jsonOpt);
        block1Cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            Environment.Exit(await DogfoodBlock1Command.ExecuteAsync(json));
        });
        AddCommand(block1Cmd);
    }
}

/// <summary>
/// Block 1 dogfood validation: Observation pipeline watches Nexo repo and stores patterns from file events.
/// </summary>
internal static class DogfoodBlock1Command
{
    public static async Task<int> ExecuteAsync(bool json)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        if (!File.Exists(slnPath))
        {
            if (json)
                Console.WriteLine("{\"block\":\"block1\",\"passed\":false,\"reason\":\"Not in Nexo repo (Nexo.sln not found)\"}");
            else
                Console.Error.WriteLine("Block 1 dogfood gate FAILED: Not in Nexo repo (Nexo.sln not found). Run from Nexo repository root.");
            return 1;
        }

        var srcDir = Path.Combine(repoRoot, "src");
        if (!Directory.Exists(srcDir))
        {
            if (json)
                Console.WriteLine("{\"block\":\"block1\",\"passed\":false,\"reason\":\"src/ directory not found\"}");
            else
                Console.Error.WriteLine("Block 1 dogfood gate FAILED: src/ directory not found.");
            return 1;
        }

        var storePath = Path.Combine(repoRoot, "nexo-dogfood-block1.db");
        var verifyFile = Path.Combine(srcDir, ".dogfood-block1-verify.tmp");

        try
        {
            var store = new LiteDbPatternStore(storePath);
            var patternDetector = new PatternDetector(
                TimeSpan.FromMinutes(5),
                3,
                store,
                null);

            var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            var fileSource = new FileSystemEventSource(
                new[] { srcDir },
                repoRoot,
                new[] { "*" },
                loggerFactory.CreateLogger<FileSystemEventSource>());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var eventCount = 0;
            var processTask = Task.Run(async () =>
            {
                await foreach (var evt in fileSource.SubscribeAsync(cts.Token).WithCancellation(cts.Token))
                {
                    eventCount++;
                    await patternDetector.ProcessAsync(evt, cts.Token);
                    if (eventCount >= 5)
                        break;
                }
            }, cts.Token);

            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v1", cts.Token);
            await Task.Delay(300, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v2", cts.Token);
            await Task.Delay(300, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v3", cts.Token);

            await processTask;

            using var queryCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var patterns = await store.QueryAsync(new PatternStoreQueryParams { MaxCount = 10 }, queryCts.Token);

            var hasRepeatedEdits = patterns.Any(p => p.EventType == "repeated-edits");
            var passed = hasRepeatedEdits;

            if (json)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                {
                    block = "block1",
                    passed,
                    reason = passed ? "Observation pipeline stored patterns from Nexo's own file events" : "No repeated-edits pattern stored",
                    patternCount = patterns.Count,
                    eventCount
                }));
            }
            else
            {
                if (passed)
                    Console.WriteLine("Block 1 dogfood gate PASSED: Observation pipeline stored patterns from Nexo's own file events.");
                else
                    Console.Error.WriteLine($"Block 1 dogfood gate FAILED: No repeated-edits pattern stored (events: {eventCount}, patterns: {patterns.Count}).");
            }

            return passed ? 0 : 1;
        }
        finally
        {
            try
            {
                if (File.Exists(verifyFile))
                    File.Delete(verifyFile);
                if (File.Exists(storePath))
                    File.Delete(storePath);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}
