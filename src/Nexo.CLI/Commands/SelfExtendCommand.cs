using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using Nexo.CLI.Commands.BackgroundAgent;

namespace Nexo.CLI.Commands;

/// <summary>
/// Runs a single self-extend cycle with an explicit objective.
/// Reuses the existing repo.fs toolbox and policy guardrails.
/// </summary>
public sealed class SelfExtendCommand : Command
{
    private readonly Func<SelfExtendRunnerAdapter> _runnerFactory;

    public SelfExtendCommand(Func<SelfExtendRunnerAdapter> runnerFactory)
        : base("self-extend", "Run one self-extend cycle with file-write tools under policy.")
    {
        _runnerFactory = runnerFactory ?? throw new ArgumentNullException(nameof(runnerFactory));

        var runCmd = new Command("run", "Run one objective-driven self-extend cycle.");
        var goalOpt = new Option<string>("--goal", "Objective for the self-extend cycle.") { IsRequired = true };
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var providerOpt = new Option<string?>("--provider", () => "mock-json", "Model provider override (default: mock-json).");
        var allowMockOpt = new Option<bool>("--allow-mock", () => true, "Enable mock/offline providers for this run.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        var runTestsOpt = new Option<bool>("--run-tests", () => false, "After scaffolding, run the internal test suite filter for generated extensions.");
        var testFilterOpt = new Option<string>("--test-filter", () => "SelfExtendGenerated", "Filter passed to `nexo test local` when --run-tests is enabled.");

        runCmd.AddOption(goalOpt);
        runCmd.AddOption(repoRootOpt);
        runCmd.AddOption(providerOpt);
        runCmd.AddOption(allowMockOpt);
        runCmd.AddOption(jsonOpt);
        runCmd.AddOption(runTestsOpt);
        runCmd.AddOption(testFilterOpt);
        runCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var goal = ctx.ParseResult.GetValueForOption(goalOpt) ?? string.Empty;
            var repoRoot = ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory;
            var provider = ctx.ParseResult.GetValueForOption(providerOpt);
            var allowMock = ctx.ParseResult.GetValueForOption(allowMockOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var runTests = ctx.ParseResult.GetValueForOption(runTestsOpt);
            var testFilter = ctx.ParseResult.GetValueForOption(testFilterOpt) ?? "SelfExtendGenerated";
            Environment.ExitCode = await ExecuteAsync(goal, repoRoot, provider, allowMock, json, runTests, testFilter, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        AddCommand(runCmd);
    }

    internal async Task<int> ExecuteAsync(
        string goal,
        string repoRoot,
        string? provider,
        bool allowMock,
        bool json,
        bool runTests,
        string testFilter,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            WriteResult(false, "Goal is required.", repoRoot, provider, executed: 0, denied: 0, json, testsRun: false, testsPassed: null, testFilter: null, testSummary: null);
            return 1;
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteResult(false, $"Repo root not found: {fullRepoRoot}", fullRepoRoot, provider, executed: 0, denied: 0, json, testsRun: false, testsPassed: null, testFilter: null, testSummary: null);
            return 1;
        }

        var previousAllowMock = Environment.GetEnvironmentVariable("NEXO_ALLOW_MOCK");
        var previousProvider = Environment.GetEnvironmentVariable("NEXO_MODEL_PROVIDER");
        try
        {
            if (allowMock)
                Environment.SetEnvironmentVariable("NEXO_ALLOW_MOCK", "1");
            if (!string.IsNullOrWhiteSpace(provider))
                Environment.SetEnvironmentVariable("NEXO_MODEL_PROVIDER", provider.Trim());

            var runner = _runnerFactory();
            var result = await runner.RunAsync(fullRepoRoot, goal, ct).ConfigureAwait(false);
            var testsRun = false;
            bool? testsPassed = null;
            string? testSummary = null;

            if (result.Success && runTests)
            {
                testsRun = true;
                var testRun = await RunGeneratedTestSuiteAsync(fullRepoRoot, testFilter, ct).ConfigureAwait(false);
                testsPassed = testRun.ExitCode == 0;
                testSummary = testsPassed.Value
                    ? $"Generated extension tests passed (filter={testFilter})."
                    : $"Generated extension tests failed (filter={testFilter}, exit={testRun.ExitCode}).";
            }

            WriteResult(
                result.Success,
                result.Summary,
                fullRepoRoot,
                provider,
                result.ToolCallsExecuted,
                result.ToolCallsDenied,
                json,
                testsRun,
                testsPassed,
                runTests ? testFilter : null,
                testSummary);
            if (!result.Success)
                return 1;
            if (testsRun && testsPassed == false)
                return 1;
            return 0;
        }
        finally
        {
            Environment.SetEnvironmentVariable("NEXO_ALLOW_MOCK", previousAllowMock);
            Environment.SetEnvironmentVariable("NEXO_MODEL_PROVIDER", previousProvider);
        }
    }

    private static void WriteResult(
        bool success,
        string summary,
        string repoRoot,
        string? provider,
        int executed,
        int denied,
        bool json,
        bool testsRun,
        bool? testsPassed,
        string? testFilter,
        string? testSummary)
    {
        if (json)
        {
            var payload = new
            {
                ok = success,
                repoRoot,
                provider = provider ?? "(default)",
                executed,
                denied,
                summary,
                tests = new
                {
                    run = testsRun,
                    passed = testsPassed,
                    filter = testFilter,
                    summary = testSummary
                }
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"self-extend: {(success ? "ok" : "failed")}");
        Console.WriteLine($"repo-root: {repoRoot}");
        Console.WriteLine($"provider: {provider ?? "(default)"}");
        Console.WriteLine($"executed: {executed}, denied: {denied}");
        Console.WriteLine(summary);
        if (testsRun)
        {
            Console.WriteLine($"tests-filter: {testFilter}");
            Console.WriteLine(testSummary ?? $"tests-passed={testsPassed}");
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunGeneratedTestSuiteAsync(
        string repoRoot,
        string testFilter,
        CancellationToken ct)
    {
        var build = await RunProcessAsync(
            "dotnet",
            repoRoot,
            new[] { "build", "src/Nexo.Tests.CLI/Nexo.Tests.CLI.csproj" },
            ct).ConfigureAwait(false);
        if (build.ExitCode != 0)
            return build;

        var run = await RunProcessAsync(
            "dotnet",
            repoRoot,
            new[]
            {
                "run",
                "--project",
                "src/Nexo.CLI",
                "--",
                "test",
                "local",
                "--filter",
                testFilter,
                "--format-json"
            },
            ct).ConfigureAwait(false);

        // Treat "0 tests discovered" as failure for generated-extension validation.
        if (run.ExitCode == 0 && run.StdOut.Contains("\"TotalTests\":0", StringComparison.Ordinal))
        {
            return (1, run.StdOut, run.StdErr + Environment.NewLine + $"No tests discovered for filter '{testFilter}'.");
        }

        return run;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        string workingDirectory,
        IEnumerable<string> args,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        if (process == null)
            return (1, string.Empty, "Failed to start test process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(stderr))
            Console.Error.WriteLine(stderr.Trim());
        return (process.ExitCode, stdout, stderr);
    }
}
