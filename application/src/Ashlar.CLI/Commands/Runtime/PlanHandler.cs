namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Handles plan requests.</summary>
internal sealed class PlanHandler(Func<string, string, string, string, string, string?, string?, int?, bool, int, RuntimePlanContext> buildPlanContext)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(
        string goal,
        string repoRoot,
        string testFilter,
        string bootstrapProfile,
        string qaPolicy,
        string? runtimeManifestPath,
        string? runtimeManifestJson,
        int? maxIterationsOverride,
        bool useHistory,
        int historyWindow,
        bool json)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, "Goal is required."), json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryNormalizeQaPolicy(qaPolicy, out _))
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, "Invalid --qa-policy. Use auto, demo, release, prod, or research."), json);
            return Task.FromResult(1);
        }

        if (!RuntimeCommandUtilities.TryNormalizeBootstrapProfile(bootstrapProfile, out _))
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, "Invalid --bootstrap-profile. Use auto, self-extend-functional, self-extend-aesthetic, or self-extend-visual."), json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        try
        {
            var context = buildPlanContext(
                goal,
                fullRepoRoot,
                testFilter,
                bootstrapProfile,
                qaPolicy,
                runtimeManifestPath,
                runtimeManifestJson,
                maxIterationsOverride,
                useHistory,
                historyWindow);
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(
                true,
                "Plan computed successfully.",
                context.Plan,
                context.WorkflowSpec,
                context.AdaptivePolicyReason), json);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, $"Failed to compute plan: {ex.Message}"), json);
            return Task.FromResult(1);
        }
    }
}
