using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Observation.Models;
using Ashlar.Core.Application.Observation.Ports;
using Ashlar.Core.Application.Paths;
using Ashlar.Infrastructure.Observation;
using Ashlar.Tools.Dev;

namespace Ashlar.CLI.Commands;

/// <summary>
/// Runs a dogfood block via dotnet test with the given filter.
/// </summary>
internal static class DogfoodTestCommand
{
    /// <summary>Creates a new Create instance.</summary>
    public static Command Create(string name, string description, string filter, Option<bool> jsonOpt)
    {
        var cmd = new Command(name, description);
        cmd.AddOption(jsonOpt);
        var verboseOpt = new Option<bool>("--verbose", () => false, "Stream test output to console");
        cmd.AddOption(verboseOpt);
        var filterOpt = new Option<string?>(
            "--filter",
            "Override the block's default FullyQualifiedName~ slice (host-proof / focused reruns)");
        cmd.AddOption(filterOpt);
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            // Local `--format-json` / `--verbose` on the block, plus the root
            // globals. A leading `--format-json dogfood block2` used to print
            // prose on a JSON pipe because only the local Option was read.
            var json = CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt);
            var verbose = CommandExecutionSupport.WantsVerbose(ctx.ParseResult, verboseOpt);
            var filterOverride = ctx.ParseResult.GetValueForOption(filterOpt);
            var effectiveFilter = string.IsNullOrWhiteSpace(filterOverride) ? filter : filterOverride.Trim();
            Environment.Exit(await ExecuteAsync(name, effectiveFilter, json, verbose));
        });
        return cmd;
    }

    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public static async Task<int> ExecuteAsync(string blockName, string filter, bool json, bool verbose = false)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Ashlar.sln");
        var testProj = Path.Combine(repoRoot, "src", "Ashlar.Tests.Infrastructure", "Ashlar.Tests.Infrastructure.csproj");

        if (!File.Exists(slnPath))
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed = false, reason = "Not in Ashlar repo (Ashlar.sln not found)" }));
            else
                Console.Error.WriteLine($"{blockName} dogfood gate FAILED: Not in Ashlar repo. Run from Ashlar repository root.");
            return 1;
        }

        if (!File.Exists(testProj))
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed = false, reason = "Ashlar.Tests.Infrastructure.csproj not found" }));
            else
                Console.Error.WriteLine($"{blockName} dogfood gate FAILED: Test project not found.");
            return 1;
        }

        var (buildResult, _, _) = await RunDotNetAsync(repoRoot, "build", verbose, testProj, "-v", "minimal");
        if (buildResult != 0)
        {
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed = false, reason = "Build failed" }));
            else
                Console.Error.WriteLine($"{blockName} dogfood gate FAILED: Build failed.");
            return 1;
        }

        var verbosity = verbose ? "normal" : "minimal";
        var (testResult, stdout, stderr) = await RunDotNetAsync(
            repoRoot,
            "test",
            verbose,
            testProj,
            "--filter",
            $"FullyQualifiedName~{filter}",
            "--no-build",
            "-v",
            verbosity);

        // Same class as `ashlar test local`: `dotnet test --filter` exits 0 when
        // discovery matches nothing.
        var executed = DotnetTestTool.HasExecutedTests(stdout, stderr);
        var passed = testResult == 0 && executed;
        var reason = passed
            ? "Tests passed"
            : testResult == 0
                ? "No tests matched the filter"
                : "Tests failed";

        if (json)
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { block = blockName, passed, reason }));
        else if (passed)
            Console.WriteLine($"{blockName} dogfood gate PASSED.");
        else
            Console.Error.WriteLine($"{blockName} dogfood gate FAILED: {reason}.");

        return passed
            ? (int)ExitCode.Ok
            : testResult == 0
                ? (int)ExitCode.ValidationFailed
                : testResult;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunDotNetAsync(
        string workingDir,
        string command,
        bool streamOutput,
        params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(command);
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi);
        if (proc == null) return (1, string.Empty, string.Empty);

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        if (streamOutput)
        {
            if (!string.IsNullOrEmpty(stdout))
                Console.Write(stdout);
            if (!string.IsNullOrEmpty(stderr))
                Console.Error.Write(stderr);
        }

        return (proc.ExitCode, stdout, stderr);
    }
}
