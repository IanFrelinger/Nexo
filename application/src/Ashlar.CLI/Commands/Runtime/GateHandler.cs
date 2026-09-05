namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Handles runtime gate requests.</summary>
internal sealed class RuntimeGateHandler
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(
        string repoRoot,
        int historyWindow,
        double minPassRate,
        int minTotal,
        string? goal,
        string? policy,
        string? benchmarkSet,
        string? stage,
        int minConsecutivePasses,
        bool json)
    {
        if (!string.IsNullOrWhiteSpace(policy) && !RuntimeCommandUtilities.TryNormalizeQaPolicy(policy, out _))
        {
            RuntimeOutputWriter.WriteGateResult(
                new RuntimeGateResult(false, "Invalid --policy. Use auto, demo, release, prod, or research."),
                json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(historyWindow))
        {
            RuntimeOutputWriter.WriteGateResult(
                new RuntimeGateResult(false, RuntimeCommandUtilities.InvalidHistoryWindowMessage),
                json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(minTotal))
        {
            RuntimeOutputWriter.WriteGateResult(
                new RuntimeGateResult(false, RuntimeCommandUtilities.InvalidMinTotalMessage),
                json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryValidateUnitInterval(minPassRate))
        {
            RuntimeOutputWriter.WriteGateResult(
                new RuntimeGateResult(false, RuntimeCommandUtilities.InvalidMinPassRateMessage),
                json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryValidateNonNegativeCount(minConsecutivePasses))
        {
            RuntimeOutputWriter.WriteGateResult(
                new RuntimeGateResult(false, RuntimeCommandUtilities.InvalidMinConsecutivePassesMessage),
                json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WriteGateResult(new RuntimeGateResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var gate = RuntimeGateEvaluation.EvaluateGateResult(
            fullRepoRoot,
            historyWindow,
            minPassRate,
            minTotal,
            goal,
            policy,
            benchmarkSet,
            stage,
            minConsecutivePasses);
        RuntimeOutputWriter.WriteGateResult(gate, json);
        return Task.FromResult(gate.Ok ? 0 : 1);
    }
}
