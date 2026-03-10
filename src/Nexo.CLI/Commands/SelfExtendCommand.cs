using System.CommandLine;
using System.CommandLine.Invocation;
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

        runCmd.AddOption(goalOpt);
        runCmd.AddOption(repoRootOpt);
        runCmd.AddOption(providerOpt);
        runCmd.AddOption(allowMockOpt);
        runCmd.AddOption(jsonOpt);
        runCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var goal = ctx.ParseResult.GetValueForOption(goalOpt) ?? string.Empty;
            var repoRoot = ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory;
            var provider = ctx.ParseResult.GetValueForOption(providerOpt);
            var allowMock = ctx.ParseResult.GetValueForOption(allowMockOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            Environment.ExitCode = await ExecuteAsync(goal, repoRoot, provider, allowMock, json, ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        AddCommand(runCmd);
    }

    internal async Task<int> ExecuteAsync(
        string goal,
        string repoRoot,
        string? provider,
        bool allowMock,
        bool json,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            WriteResult(false, "Goal is required.", repoRoot, provider, executed: 0, denied: 0, json);
            return 1;
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteResult(false, $"Repo root not found: {fullRepoRoot}", fullRepoRoot, provider, executed: 0, denied: 0, json);
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
            WriteResult(
                result.Success,
                result.Summary,
                fullRepoRoot,
                provider,
                result.ToolCallsExecuted,
                result.ToolCallsDenied,
                json);
            return result.Success ? 0 : 1;
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
        bool json)
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
                summary
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"self-extend: {(success ? "ok" : "failed")}");
        Console.WriteLine($"repo-root: {repoRoot}");
        Console.WriteLine($"provider: {provider ?? "(default)"}");
        Console.WriteLine($"executed: {executed}, denied: {denied}");
        Console.WriteLine(summary);
    }
}
