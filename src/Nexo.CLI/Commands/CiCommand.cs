using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Nexo.Core.Application.Paths;

namespace Nexo.CLI.Commands;

/// <summary>
/// CI verification: build + smoke tests + architecture validation.
/// Replaces scripts/ci-verify.sh. Used by make ci-verify.
/// </summary>
public sealed class CiCommand : Command
{
    public CiCommand() : base("ci", "CI verification: build, smoke tests, and architecture validation")
    {
        var verifyCmd = new Command("verify", "Run full CI verification (build, test, validate)");
        verifyCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var exitCode = await ExecuteVerifyAsync();
            Environment.Exit(exitCode);
        });
        AddCommand(verifyCmd);

        var runtimeGateCmd = new Command("runtime-gate", "Run runtime release gate benchmark and SLO checks.");
        runtimeGateCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var exitCode = await ExecuteRuntimeGateAsync();
            Environment.Exit(exitCode);
        });
        AddCommand(runtimeGateCmd);

        var runtimePromotionCmd = new Command("runtime-promotion", "Run strict runtime promotion gate profile.");
        runtimePromotionCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var exitCode = await ExecuteRuntimePromotionAsync();
            Environment.Exit(exitCode);
        });
        AddCommand(runtimePromotionCmd);
    }

    /// <summary>
    /// Runs: dotnet build → dotnet test (smoke) → nexo validate.
    /// Exits 0 only if all succeed.
    /// </summary>
    public static async Task<int> ExecuteVerifyAsync()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var cliProject = Path.Combine(repoRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        var infraTestsProject = Path.Combine(repoRoot, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        if (!File.Exists(cliProject) || !File.Exists(infraTestsProject))
        {
            Console.Error.WriteLine("ci verify: Required projects not found. Run from repository root.");
            return 1;
        }

        // Step 1: Build targeted projects
        Console.WriteLine("=== CI Verify: Build CLI ===");
        var buildCliExit = await RunProcessAsync("dotnet", $"build \"{cliProject}\"", repoRoot);
        if (buildCliExit != 0)
        {
            Console.Error.WriteLine($"ci verify: CLI build failed (exit {buildCliExit})");
            return buildCliExit;
        }

        Console.WriteLine("=== CI Verify: Build Infrastructure Tests ===");
        var buildInfraExit = await RunProcessAsync("dotnet", $"build \"{infraTestsProject}\"", repoRoot);
        if (buildInfraExit != 0)
        {
            Console.Error.WriteLine($"ci verify: Infrastructure test build failed (exit {buildInfraExit})");
            return buildInfraExit;
        }

        // Step 2: Smoke tests (BaseFrameworkSmokeTests)
        Console.WriteLine("=== CI Verify: Smoke Tests ===");
        var testExit = await RunProcessAsync(
            "dotnet",
            $"test \"{infraTestsProject}\" --no-build --blame-hang-timeout 30s --blame-hang-dump-type none --filter \"FullyQualifiedName~BaseFrameworkSmokeTests\" --verbosity minimal",
            repoRoot);
        if (testExit != 0)
        {
            Console.Error.WriteLine($"ci verify: Smoke tests failed (exit {testExit})");
            return testExit;
        }

        // Step 3: Architecture validation (nexo validate)
        Console.WriteLine("=== CI Verify: Architecture Validation ===");
        var validateExit = await RunProcessAsync(
            "dotnet",
            $"run --project \"{cliProject}\" -- validate",
            repoRoot);
        if (validateExit != 0)
        {
            Console.Error.WriteLine($"ci verify: Architecture validation failed (exit {validateExit})");
            return validateExit;
        }

        Console.WriteLine("=== CI Verify: All checks passed ===");
        return 0;
    }

    public static async Task<int> ExecuteRuntimeGateAsync()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var cliProject = Path.Combine(repoRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        if (!File.Exists(cliProject))
        {
            Console.Error.WriteLine($"ci runtime-gate: Missing CLI project: {cliProject}");
            return 1;
        }

        Console.WriteLine("=== CI Runtime Gate ===");
        var exit = await RunProcessAsync(
            "dotnet",
            $"run --project \"{cliProject}\" -- runtime release-gate --repo-root \"{repoRoot}\" --mode full --core-min-total 3 --core-history-window 3 --visual-required-mode false --visual-min-total 3 --visual-history-window 3",
            repoRoot);
        if (exit != 0)
            Console.Error.WriteLine($"ci runtime-gate: Failed (exit {exit})");
        return exit;
    }

    public static async Task<int> ExecuteRuntimePromotionAsync()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var cliProject = Path.Combine(repoRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        if (!File.Exists(cliProject))
        {
            Console.Error.WriteLine($"ci runtime-promotion: Missing CLI project: {cliProject}");
            return 1;
        }

        Console.WriteLine("=== CI Runtime Promotion ===");
        var exit = await RunProcessAsync(
            "dotnet",
            $"run --project \"{cliProject}\" -- runtime release-gate --repo-root \"{repoRoot}\" --mode full --lane-repetitions 3 --core-min-pass-rate 0.9 --core-min-total 9 --core-history-window 9 --visual-required-mode true --visual-min-pass-rate 0.85 --visual-min-total 9 --visual-history-window 9 --visual-promotion-streak 3",
            repoRoot);
        if (exit != 0)
            Console.Error.WriteLine($"ci runtime-promotion: Failed (exit {exit})");
        return exit;
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
