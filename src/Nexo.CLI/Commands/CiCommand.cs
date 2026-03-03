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
    }

    /// <summary>
    /// Runs: dotnet build → dotnet test (smoke) → nexo validate.
    /// Exits 0 only if all succeed.
    /// </summary>
    public static async Task<int> ExecuteVerifyAsync()
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var slnPath = Path.Combine(repoRoot, "Nexo.sln");
        if (!File.Exists(slnPath))
        {
            Console.Error.WriteLine("ci verify: Not in Nexo repo (Nexo.sln not found). Run from repository root.");
            return 1;
        }

        // Step 1: Build
        Console.WriteLine("=== CI Verify: Build ===");
        var buildExit = await RunProcessAsync("dotnet", "build", repoRoot);
        if (buildExit != 0)
        {
            Console.Error.WriteLine($"ci verify: Build failed (exit {buildExit})");
            return buildExit;
        }

        // Step 2: Smoke tests (BaseFrameworkSmokeTests)
        Console.WriteLine("=== CI Verify: Smoke Tests ===");
        var testProject = Path.Combine(repoRoot, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        var testExit = await RunProcessAsync(
            "dotnet",
            $"test \"{testProject}\" --no-build --blame-hang-timeout 30s --blame-hang-dump-type none --filter \"FullyQualifiedName~BaseFrameworkSmokeTests\" --verbosity minimal",
            repoRoot);
        if (testExit != 0)
        {
            Console.Error.WriteLine($"ci verify: Smoke tests failed (exit {testExit})");
            return testExit;
        }

        // Step 3: Architecture validation (nexo validate)
        Console.WriteLine("=== CI Verify: Architecture Validation ===");
        var cliProject = Path.Combine(repoRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
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
