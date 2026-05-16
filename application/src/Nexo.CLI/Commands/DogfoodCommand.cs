using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
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

        AddCommand(DogfoodTestCommand.Create("block2", "Block 2: static analyzer runs against Block 1 (Observation) code", "DogfoodBlock2Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block3", "Block 3: adaptation engine decomposes/recompiles Nexo brick", "DogfoodBlock3Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block4", "Block 4: promote Nexo fix via inheritance", "DogfoodBlock4Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block5", "Block 5: autonomy controls on Nexo dev workflow", "DogfoodBlock5Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block6", "Block 6: SelfContextAssembler answers 24h question", "DogfoodBlock6Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block7", "Block 7: Composition engine composes for Nexo problem", "DogfoodBlock7Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block8", "Block 8: Parallel test matrix against Nexo tests", "DogfoodBlock8Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("block9", "Block 9: Instance mesh discover/advertise", "DogfoodBlock9Tests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("closedloop", "Closed-loop improve on Nexo", "DogfoodClosedLoopTests", jsonOpt));
        AddCommand(DogfoodTestCommand.Create("phasef", "Phase F: changelog, test failure store", "DogfoodPhaseFTests", jsonOpt));

        var allCmd = new Command("all", "Run all dogfood blocks (1–9) + closedloop + phasef");
        allCmd.AddOption(jsonOpt);
        var verboseOpt = new Option<bool>("--verbose", () => false, "Stream test output to console");
        allCmd.AddOption(verboseOpt);
        allCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            Environment.Exit(await DogfoodAllCommand.ExecuteAsync(json, verbose));
        });
        AddCommand(allCmd);
    }
}

/// <summary>
/// Runs a dogfood block via dotnet test with the given filter.
/// </summary>
internal static class DogfoodTestCommand
{
    public static Command Create(string name, string description, string filter, Option<bool> jsonOpt)
    {
        var cmd = new Command(name, description);
        cmd.AddOption(jsonOpt);
        var verboseOpt = new Option<bool>("--verbose", () => false, "Stream test output to console");
        cmd.AddOption(verboseOpt);
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            Environment.Exit(await ExecuteAsync(name, filter, json, verbose));
        });
        return cmd;
    }

    public static async Task<int> ExecuteAsync(string blockName, string filter, bool json, bool verbose = false)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        var testProj = Path.Combine(repoRoot, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");

        if (!File.Exists(slnPath))
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed = false, reason = "Not in Nexo repo (Nexo.sln not found)" }));
            else
                Console.Error.WriteLine($"{blockName} dogfood gate FAILED: Not in Nexo repo. Run from Nexo repository root.");
            return 1;
        }

        if (!File.Exists(testProj))
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed = false, reason = "Nexo.Tests.Infrastructure.csproj not found" }));
            else
                Console.Error.WriteLine($"{blockName} dogfood gate FAILED: Test project not found.");
            return 1;
        }

        var buildResult = await RunDotNetAsync(repoRoot, "build", verbose, testProj, "-v", "minimal");
        if (buildResult != 0)
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed = false, reason = "Build failed" }));
            else
                Console.Error.WriteLine($"{blockName} dogfood gate FAILED: Build failed.");
            return 1;
        }

        var verbosity = verbose ? "normal" : "minimal";
        var testResult = await RunDotNetAsync(repoRoot, "test", verbose, testProj, "--filter", $"FullyQualifiedName~{filter}", "--no-build", "-v", verbosity);
        var passed = testResult == 0;

        if (json)
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed, reason = passed ? "Tests passed" : "Tests failed" }));
        else if (passed)
            Console.WriteLine($"{blockName} dogfood gate PASSED.");
        else
            Console.Error.WriteLine($"{blockName} dogfood gate FAILED.");

        return testResult;
    }

    private static async Task<int> RunDotNetAsync(string workingDir, string command, bool streamOutput, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
        };
        if (!streamOutput)
        {
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
        }
        psi.ArgumentList.Add(command);
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi);
        if (proc == null) return 1;
        // When streamOutput: no redirect, child inherits console and output streams directly
        await proc.WaitForExitAsync();
        return proc.ExitCode;
    }
}

/// <summary>
/// Runs all dogfood blocks in sequence. Reports pass/fail per block; does not stop on first failure.
/// </summary>
internal static class DogfoodAllCommand
{
    private static readonly (string Name, string Filter)[] Blocks =
    {
        ("block1", ""),
        ("block2", "DogfoodBlock2Tests"),
        ("block3", "DogfoodBlock3Tests"),
        ("block4", "DogfoodBlock4Tests"),
        ("block5", "DogfoodBlock5Tests"),
        ("block6", "DogfoodBlock6Tests"),
        ("block7", "DogfoodBlock7Tests"),
        ("block8", "DogfoodBlock8Tests"),
        ("block9", "DogfoodBlock9Tests"),
        ("closedloop", "DogfoodClosedLoopTests"),
        ("phasef", "DogfoodPhaseFTests"),
    };

    public static async Task<int> ExecuteAsync(bool json, bool verbose = false)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        if (!File.Exists(slnPath))
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { passed = false, reason = "Not in Nexo repo" }));
            else
                Console.Error.WriteLine("dogfood all FAILED: Not in Nexo repo. Run from Nexo repository root.");
            return 1;
        }

        var results = new List<(string Block, bool Passed)>();
        foreach (var (name, filter) in Blocks)
        {
            int exitCode;
            if (name == "block1")
                exitCode = await DogfoodBlock1Command.ExecuteAsync(json);
            else
                exitCode = await DogfoodTestCommand.ExecuteAsync(name, filter, json, verbose);

            results.Add((name, exitCode == 0));
        }

        var allPassed = results.All(r => r.Passed);
        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                passed = allPassed,
                blocks = results.Select(r => new { block = r.Block, passed = r.Passed }).ToArray(),
            }));
        }
        else
        {
            foreach (var (block, passed) in results)
                Console.WriteLine($"  {block}: {(passed ? "PASSED" : "FAILED")}");
            Console.WriteLine(allPassed ? "dogfood all PASSED." : "dogfood all FAILED (one or more blocks failed).");
        }

        return allPassed ? 0 : 1;
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

        var storePath = Path.Combine(Path.GetTempPath(), $"nexo-dogfood-block1-{Guid.NewGuid():N}.db");
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
            await Task.Delay(500, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v2", cts.Token);
            await Task.Delay(500, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v3", cts.Token);
            await Task.Delay(500, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v4", cts.Token);
            await Task.Delay(500, cts.Token);
            await File.WriteAllTextAsync(verifyFile, "// dogfood block1 v5", cts.Token);

            await processTask;

            // Allow pattern detector to process events (FileSystemWatcher can batch/delay on macOS)
            await Task.Delay(1500, cts.Token);

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
