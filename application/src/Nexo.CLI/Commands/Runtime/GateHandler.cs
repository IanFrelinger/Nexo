namespace Nexo.CLI.Commands;

internal sealed class RuntimeGateHandler
{
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
