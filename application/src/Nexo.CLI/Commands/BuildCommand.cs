using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Nexo.Spike.S0;
using Nexo.Spike.S0.Models;

namespace Nexo.CLI.Commands;

/// <summary>
/// Synchronous S0 spike entry point: blocks and streams the SPEC/RED/GREEN/VERIFY loop.
/// </summary>
public sealed class BuildCommand : Command
{
    public BuildCommand() : base("build", "Run spike S0 TDD loop (SPEC/RED/GREEN/VERIFY) synchronously.")
    {
        var intentOpt = new Option<FileInfo>(
            "--intent",
            "Brick intent JSON (structured spec).")
        { IsRequired = true };

        var workspaceOpt = new Option<string>(
            "--workspace",
            () => Path.Combine(Path.GetTempPath(), "nexo-spike-s0"),
            "Fresh brick workspace directory.");

        var thresholdOpt = new Option<double>(
            "--mutation-threshold",
            () => 80,
            "Minimum Stryker mutation score (percent) to pass VERIFY.");

        var maxBouncesOpt = new Option<int>(
            "--max-bounces",
            () => 3,
            "Maximum RED↔GREEN bounces before budget exhaustion.");

        var commitOpt = new Option<bool>(
            "--commit",
            () => false,
            "Create a git commit at each stage.");

        var skipMutationOpt = new Option<bool>(
            "--skip-mutation",
            () => false,
            "Skip Stryker VERIFY (for fast gate checks).");

        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable run log JSON.");

        AddOption(intentOpt);
        AddOption(workspaceOpt);
        AddOption(thresholdOpt);
        AddOption(maxBouncesOpt);
        AddOption(commitOpt);
        AddOption(skipMutationOpt);
        AddOption(jsonOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var intent = ctx.ParseResult.GetValueForOption(intentOpt)!;
            var workspace = ctx.ParseResult.GetValueForOption(workspaceOpt)!;
            var threshold = ctx.ParseResult.GetValueForOption(thresholdOpt);
            var maxBounces = ctx.ParseResult.GetValueForOption(maxBouncesOpt);
            var commit = ctx.ParseResult.GetValueForOption(commitOpt);
            var skipMutation = ctx.ParseResult.GetValueForOption(skipMutationOpt);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);

            ctx.ExitCode = await ExecuteAsync(
                intent.FullName,
                workspace,
                threshold,
                maxBounces,
                commit,
                skipMutation,
                json,
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
    }

    internal static async Task<int> ExecuteAsync(
        string intentPath,
        string workspace,
        double mutationThreshold,
        int maxBounces,
        bool commitPerStage,
        bool skipMutation,
        bool json,
        CancellationToken ct)
    {
        if (!File.Exists(intentPath))
        {
            Console.Error.WriteLine($"Intent not found: {intentPath}");
            return (int)ExitCode.ValidationFailed;
        }

        var options = new SpikeLoopOptions
        {
            IntentPath = intentPath,
            WorkspaceRoot = workspace,
            MutationThresholdPercent = mutationThreshold,
            MaxRedGreenBounces = maxBounces,
            CommitPerStage = commitPerStage,
            SkipMutation = skipMutation
        };

        var runner = new TddLoopRunner();
        var log = await runner.RunAsync(options, ct).ConfigureAwait(false);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            WriteHumanSummary(log, workspace);
        }

        return log.Outcome switch
        {
            SpikeRunOutcome.Success => (int)ExitCode.Ok,
            SpikeRunOutcome.Blocked => (int)ExitCode.ValidationFailed,
            SpikeRunOutcome.Rejected => (int)ExitCode.ValidationFailed,
            SpikeRunOutcome.BudgetExhausted => (int)ExitCode.ValidationFailed,
            _ => (int)ExitCode.UnexpectedError
        };
    }

    private static void WriteHumanSummary(SpikeRunLog log, string workspace)
    {
        Console.WriteLine($"S0 spike run {log.RunId}");
        Console.WriteLine($"  Intent:     {log.IntentId}");
        Console.WriteLine($"  Outcome:    {log.Outcome}");
        Console.WriteLine($"  Guard:      {log.RejectingGuard}");
        Console.WriteLine($"  Stage:      {log.RejectedAtStage?.ToString() ?? "(none)"}");
        Console.WriteLine($"  Bounces:    {log.RedGreenBounces}");
        Console.WriteLine($"  Workspace:  {workspace}");
        Console.WriteLine($"  Log:        {Path.Combine(workspace, "spike-run-log.json")}");

        foreach (var stage in log.Stages)
            Console.WriteLine($"  [{stage.Stage}] {stage.Summary}");

        foreach (var verify in log.VerifyPasses)
            Console.WriteLine($"  [VERIFY] mutation={verify.MutationScore:F1}% passed={verify.Passed} survivors={verify.SurvivingMutants.Count}");
    }
}
