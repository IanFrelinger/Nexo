using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Handles recommend requests.</summary>
internal sealed class RecommendHandler
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(
        string goal,
        string repoRoot,
        int historyWindow,
        bool json)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            RuntimeOutputWriter.WriteRecommendResult(new RuntimeRecommendResult(false, "Goal is required."), json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(historyWindow))
        {
            RuntimeOutputWriter.WriteRecommendResult(new RuntimeRecommendResult(false, RuntimeCommandUtilities.InvalidHistoryWindowMessage), json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WriteRecommendResult(new RuntimeRecommendResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var history = AdaptiveRuntimeExecutionHistoryStore.ReadRecent(fullRepoRoot, historyWindow);
        var recommendation = AdaptiveRuntimePolicyAdvisor.RecommendQaPolicy(goal, history);
        var result = recommendation == null
            ? new RuntimeRecommendResult(true, "No recommendation from history.", Policy: null, Reason: null)
            : new RuntimeRecommendResult(true, "Recommendation computed from history.", recommendation.QaPolicy, recommendation.Reason);
        RuntimeOutputWriter.WriteRecommendResult(result, json);
        return Task.FromResult(0);
    }
}
