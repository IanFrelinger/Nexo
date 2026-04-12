using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
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

        var releaseBundleCmd = new Command("release-bundle", "Run a single-shot release gate bundle and emit unified report.");
        var profileOpt = new Option<string>("--profile", () => "default", "Bundle profile: default | quick.");
        var outputDirOpt = new Option<string?>("--output-dir", "Optional output directory for unified report artifacts.");
        releaseBundleCmd.AddOption(profileOpt);
        releaseBundleCmd.AddOption(outputDirOpt);
        releaseBundleCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var profile = ctx.ParseResult.GetValueForOption(profileOpt) ?? "default";
            var outputDir = ctx.ParseResult.GetValueForOption(outputDirOpt);
            var exitCode = await ExecuteReleaseBundleAsync(profile, outputDir);
            Environment.Exit(exitCode);
        });
        AddCommand(releaseBundleCmd);
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
            $"run --project \"{cliProject}\" -- runtime release-gate --repo-root \"{repoRoot}\" --mode full --core-min-total 3 --core-history-window 3 --visual-required-mode false --visual-min-total 3 --visual-history-window 3 --emit-slo-evidence --slo-evidence-path \"{Path.Combine(repoRoot, ".nexo", "runtime", "runtime-release-gate-slo.json")}\" --ncr-resolution-ms-slo 250 --ncr-load-ms-slo 1000 --ncr-outcome-ms-slo 1500 --ncr-failure-rate-slo 0.2",
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
            $"run --project \"{cliProject}\" -- runtime release-gate --repo-root \"{repoRoot}\" --mode full --lane-repetitions 3 --core-min-pass-rate 0.9 --core-min-total 9 --core-history-window 9 --visual-required-mode true --visual-min-pass-rate 0.85 --visual-min-total 9 --visual-history-window 9 --visual-promotion-streak 3 --emit-slo-evidence --slo-evidence-path \"{Path.Combine(repoRoot, ".nexo", "runtime", "runtime-release-promotion-slo.json")}\" --ncr-resolution-ms-slo 200 --ncr-load-ms-slo 900 --ncr-outcome-ms-slo 1200 --ncr-failure-rate-slo 0.1",
            repoRoot);
        if (exit != 0)
            Console.Error.WriteLine($"ci runtime-promotion: Failed (exit {exit})");
        return exit;
    }

    public static async Task<int> ExecuteReleaseBundleAsync(string profile = "default", string? outputDirectory = null)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var cliProject = Path.Combine(repoRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        if (!File.Exists(cliProject))
        {
            Console.Error.WriteLine($"ci release-bundle: Missing CLI project: {cliProject}");
            return 1;
        }

        var normalizedProfile = string.IsNullOrWhiteSpace(profile) ? "default" : profile.Trim().ToLowerInvariant();
        var steps = BuildReleaseBundleSteps(normalizedProfile, cliProject, repoRoot);
        if (steps.Count == 0)
        {
            Console.Error.WriteLine($"ci release-bundle: Unknown profile '{profile}'. Supported profiles: default, quick.");
            return 1;
        }

        var results = new List<ReleaseBundleStepResult>();
        foreach (var step in steps)
        {
            Console.WriteLine($"=== Release Bundle: {step.Name} ===");
            var startedUtc = DateTimeOffset.UtcNow;
            var exitCode = await RunProcessAsync(step.FileName, step.Arguments, repoRoot);
            var endedUtc = DateTimeOffset.UtcNow;
            var artifactHints = step.ArtifactHints
                .Select(hint => Path.GetFullPath(Path.Combine(repoRoot, hint)))
                .ToArray();
            results.Add(new ReleaseBundleStepResult(
                step.Id,
                step.Name,
                step.FileName,
                step.Arguments,
                exitCode,
                startedUtc,
                endedUtc,
                artifactHints));
        }

        var verdict = results.All(result => result.ExitCode == 0) ? "PASS" : "FAIL";
        var reportDirectory = ResolveReleaseBundleOutputDirectory(repoRoot, outputDirectory);
        Directory.CreateDirectory(reportDirectory);
        var jsonPath = Path.Combine(reportDirectory, "release-bundle-report.json");
        var markdownPath = Path.Combine(reportDirectory, "release-bundle-report.md");
        var report = new ReleaseBundleReport(
            normalizedProfile,
            DateTimeOffset.UtcNow,
            verdict,
            results,
            jsonPath,
            markdownPath);

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(jsonPath, json).ConfigureAwait(false);

        var markdown = BuildReleaseBundleMarkdown(report);
        await File.WriteAllTextAsync(markdownPath, markdown).ConfigureAwait(false);

        Console.WriteLine($"=== Release Bundle Verdict: {verdict} ===");
        Console.WriteLine($"JSON report: {jsonPath}");
        Console.WriteLine($"Markdown report: {markdownPath}");

        return verdict == "PASS" ? 0 : 1;
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

    private static List<ReleaseBundleStep> BuildReleaseBundleSteps(string profile, string cliProject, string repoRoot)
    {
        var defaultSteps = new List<ReleaseBundleStep>
        {
            new(
                "verify",
                "CI verify",
                "dotnet",
                $"run --project \"{cliProject}\" -- ci verify",
                [
                    ".nexo/runtime/release-gate/last-run/runtime-release-gate-slo.json",
                    ".nexo/runtime/release-gate/last-run/runtime-release-gate-slo.md"
                ]),
            new(
                "runtime-gate",
                "Runtime gate",
                "dotnet",
                $"run --project \"{cliProject}\" -- ci runtime-gate",
                [
                    ".nexo/runtime/release-gate/last-run/runtime-release-gate-slo.json",
                    ".nexo/runtime/release-gate/last-run/runtime-release-gate-slo.md"
                ]),
            new(
                "doctor",
                "Doctor check",
                "dotnet",
                $"run --project \"{cliProject}\" -- doctor --json",
                [
                    "docs/OnboardingAutomation.md"
                ])
        };

        if (profile == "quick")
            return defaultSteps.Where(step => step.Id != "verify").ToList();
        if (profile == "default")
            return defaultSteps;
        return new List<ReleaseBundleStep>();
    }

    internal static IReadOnlyList<string> GetReleaseBundleStepIds(string profile)
    {
        var repoRoot = RepoPathResolver.FindRepoRoot();
        var cliProject = Path.Combine(repoRoot, "src", "Nexo.CLI", "Nexo.CLI.csproj");
        return BuildReleaseBundleSteps(profile.Trim().ToLowerInvariant(), cliProject, repoRoot)
            .Select(step => step.Id)
            .ToList();
    }

    private static string ResolveReleaseBundleOutputDirectory(string repoRoot, string? requestedOutputDirectory)
    {
        if (!string.IsNullOrWhiteSpace(requestedOutputDirectory))
            return Path.GetFullPath(requestedOutputDirectory, repoRoot);
        return Path.Combine(repoRoot, ".nexo", "release-bundle", "last-run");
    }

    private static string BuildReleaseBundleMarkdown(ReleaseBundleReport report)
    {
        var lines = new List<string>
        {
            "# Release bundle report",
            string.Empty,
            $"Profile: `{report.Profile}`",
            $"Generated: `{report.GeneratedUtc:O}`",
            $"Verdict: **{report.Verdict}**",
            string.Empty,
            "| Step | Exit code | Started (UTC) | Ended (UTC) | Artifacts |",
            "|------|-----------|---------------|-------------|-----------|"
        };

        foreach (var step in report.Steps)
        {
            var artifacts = step.ArtifactHints.Length == 0
                ? "n/a"
                : string.Join("<br/>", step.ArtifactHints.Select(path => $"`{path}`"));
            lines.Add($"| {step.Name} | {step.ExitCode} | {step.StartedUtc:O} | {step.EndedUtc:O} | {artifacts} |");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private sealed record ReleaseBundleStep(
        string Id,
        string Name,
        string FileName,
        string Arguments,
        IReadOnlyList<string> ArtifactHints);

    private sealed record ReleaseBundleStepResult(
        string Id,
        string Name,
        string FileName,
        string Arguments,
        int ExitCode,
        DateTimeOffset StartedUtc,
        DateTimeOffset EndedUtc,
        string[] ArtifactHints);

    private sealed record ReleaseBundleReport(
        string Profile,
        DateTimeOffset GeneratedUtc,
        string Verdict,
        IReadOnlyList<ReleaseBundleStepResult> Steps,
        string JsonReportPath,
        string MarkdownReportPath);
}
