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

    /// <summary>Executes the command handler and returns a process exit code.</summary>
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
