using Nexo.Spike.S0;
using Nexo.Spike.S0.Guards;
using Nexo.Spike.S0.Models;

namespace Nexo.Spike.S2.Gates;

public sealed record FullGateStackResult(
    bool AllPassed,
    IReadOnlyList<string> GatesPassed,
    string? FailedGate,
    string Detail,
    bool MutationSkipped);

/// <summary>
/// Runs the S1 verification envelope: RED discipline + PropertyGate + MutationGate.
/// </summary>
public sealed class FullGateStack
{
    private readonly PropertyGate _propertyGate = new();
    private readonly MutationGate _mutationGate = new();

    public async Task<FullGateStackResult> EvaluateAsync(
        string workspaceRoot,
        double mutationThresholdPercent,
        bool requireMutation,
        CancellationToken ct = default)
    {
        var passed = new List<string>();

        var (buildCode, buildOut, buildErr, buildTimedOut) =
            await SpikeWorkspaceScaffold.BuildAsync(workspaceRoot, ct).ConfigureAwait(false);
        if (buildCode != 0 || buildTimedOut)
        {
            return new FullGateStackResult(
                false,
                passed,
                "Build",
                TrimForReport(buildOut, buildErr),
                false);
        }

        passed.Add("Build");

        var (testCode, testOut, testErr, testTimedOut) =
            await SpikeWorkspaceScaffold.TestWithRebuildAsync(workspaceRoot, ct).ConfigureAwait(false);
        if (testCode != 0 || testTimedOut)
        {
            var reason = RedReasonClassifier.Classify(testCode, testOut, testErr, testTimedOut);
            if (TautologyGuard.IsTautological(reason))
            {
                return new FullGateStackResult(
                    false,
                    passed,
                    "RED",
                    "Tautology guard: tests pass against stub without assertion discipline",
                    false);
            }

            return new FullGateStackResult(
                false,
                passed,
                "RED",
                TrimForReport(testOut, testErr),
                false);
        }

        passed.Add("RED");

        var propertyResult = await _propertyGate.RunAsync(workspaceRoot, ct).ConfigureAwait(false);
        if (!propertyResult.Passed)
        {
            return new FullGateStackResult(
                false,
                passed,
                "PropertyGate",
                propertyResult.RawOutput,
                false);
        }

        passed.Add("PropertyGate");

        var (strykerAvailable, _) = await ProbeStrykerAsync(ct).ConfigureAwait(false);
        if (!strykerAvailable)
        {
            if (requireMutation)
            {
                return new FullGateStackResult(
                    false,
                    passed,
                    "MutationGate",
                    "dotnet-stryker unavailable but required for full gate stack",
                    true);
            }

            return new FullGateStackResult(true, passed, null, "MutationGate skipped (stryker unavailable)", true);
        }

        var mutationResult = await _mutationGate.RunAsync(workspaceRoot, mutationThresholdPercent, ct)
            .ConfigureAwait(false);
        if (!mutationResult.Passed)
        {
            return new FullGateStackResult(
                false,
                passed,
                "MutationGate",
                mutationResult.RawOutput,
                false);
        }

        passed.Add("MutationGate");
        return new FullGateStackResult(true, passed, null, mutationResult.RawOutput, false);
    }

    internal static async Task<(bool Available, string? Detail)> ProbeStrykerAsync(CancellationToken ct)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet", "stryker --help")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
                return (false, "dotnet stryker process could not start");

            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            return process.ExitCode == 0
                ? (true, "dotnet stryker --help")
                : (false, "dotnet stryker --help failed");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string TrimForReport(string stdout, string stderr, int max = 500)
    {
        var combined = $"{stdout}\n{stderr}".Trim();
        return combined.Length <= max ? combined : combined[..max] + "...";
    }
}
