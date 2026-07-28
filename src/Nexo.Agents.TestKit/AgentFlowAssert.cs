using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Agents.TestKit;

/// <summary>
/// Raised when an agent-flow expectation fails. Carries the control-flow story,
/// not just a boolean, so a failure says WHICH PATH was taken.
/// </summary>
public sealed class AgentFlowAssertionException : Exception
{
    /// <summary>Creates the exception.</summary>
    public AgentFlowAssertionException(string message) : base(message) { }
}

/// <summary>
/// Assertions about CONTROL FLOW rather than a single result bool: how many attempts
/// the loop took, which path it went down, whether it shipped or rolled back, and
/// whether a rollback actually restored prior state.
/// </summary>
public static class AgentFlowAssert
{
    /// <summary>Asserts the drafter was invoked exactly <paramref name="expected"/> times.</summary>
    public static void AttemptCount(FakeArtifactDrafter drafter, int expected)
    {
        if (drafter.Attempts != expected)
        {
            throw new AgentFlowAssertionException(
                $"Expected {expected} draft attempt(s) but the loop made {drafter.Attempts}. "
                + FeedbackStory(drafter));
        }
    }

    /// <summary>Asserts the loop converged within <paramref name="bound"/> attempts.</summary>
    public static void ConvergedWithin(FakeArtifactDrafter drafter, int bound)
    {
        if (drafter.Attempts > bound)
        {
            throw new AgentFlowAssertionException(
                $"Expected convergence within {bound} attempt(s) but the loop made {drafter.Attempts}. "
                + FeedbackStory(drafter));
        }
    }

    /// <summary>
    /// Asserts the repair loop actually fed the prior failure back: the first attempt
    /// sees no feedback, and every later attempt sees some.
    /// </summary>
    public static void FedFailureBack(FakeArtifactDrafter drafter)
    {
        if (drafter.Attempts < 2)
        {
            throw new AgentFlowAssertionException(
                "Expected at least two attempts so failure could be fed back, but the loop made "
                + drafter.Attempts + ".");
        }

        for (var i = 1; i < drafter.FeedbackPerAttempt.Count; i++)
        {
            if (drafter.FeedbackPerAttempt[i].Count == 0)
            {
                throw new AgentFlowAssertionException(
                    $"Attempt {i + 1} was redrafted with NO prior feedback — the repair loop is drafting blind. "
                    + FeedbackStory(drafter));
            }
        }
    }

    /// <summary>Asserts the model path was never taken (a genuinely offline run).</summary>
    public static void ModelWasNeverCalled(FakeProviderFactory providers)
    {
        if (!providers.WasNeverCalled)
        {
            throw new AgentFlowAssertionException(
                "Expected an offline run but the model was called with: "
                + string.Join(" | ", providers.Prompts.Select(Truncate)));
        }
    }

    /// <summary>
    /// Asserts a scaffold-level smoke failure did NOT send the model back to work —
    /// re-drafting on an environment failure is thrashing, not repair.
    /// </summary>
    public static void DidNotThrashTheModel(FakeArtifactDrafter drafter, int maxAttempts = 1)
    {
        if (drafter.Attempts > maxAttempts)
        {
            throw new AgentFlowAssertionException(
                $"Expected at most {maxAttempts} draft attempt(s) for a non-draft failure, but the model was "
                + $"re-invoked {drafter.Attempts} time(s) — the loop is thrashing the model on a failure it "
                + "cannot fix. " + FeedbackStory(drafter));
        }
    }

    /// <summary>Asserts the deployment shipped.</summary>
    public static void Shipped(FakeDeploymentTarget target)
    {
        var last = LastVerdict(target);
        if (last.Decision != AcceptanceDecision.Ship)
            throw new AgentFlowAssertionException("Expected Ship but got Rollback: " + last.Reason);
    }

    /// <summary>Asserts the deployment rolled back.</summary>
    public static void RolledBack(FakeDeploymentTarget target)
    {
        var last = LastVerdict(target);
        if (last.Decision != AcceptanceDecision.Rollback)
            throw new AgentFlowAssertionException("Expected Rollback but got Ship: " + last.Reason);
    }

    /// <summary>
    /// Asserts a rollback restored BOTH edges it is responsible for: the managed file
    /// set is back to its pre-apply contents and the prior image is live again.
    /// </summary>
    public static void RollbackRestoredPriorState(
        FakeDeploymentTarget target,
        FakeManagedFileSet files,
        IReadOnlyDictionary<string, string> expectedContents)
    {
        RolledBack(target);

        if (files.RollbackCalls == 0)
            throw new AgentFlowAssertionException("Rollback decision was reached but the file set was never rolled back.");

        if (!target.PriorImageRestored || target.CurrentImage != target.PriorImage)
        {
            throw new AgentFlowAssertionException(
                $"Rollback did not restore the prior image: live tag is '{target.CurrentImage}', "
                + $"expected '{target.PriorImage}'.");
        }

        foreach (var (key, expected) in expectedContents)
        {
            var actual = files.Contents.TryGetValue(key, out var v) ? v : null;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new AgentFlowAssertionException(
                    $"Rollback left '{key}' as '{Truncate(actual ?? "<absent>")}' but the pre-apply content was "
                    + $"'{Truncate(expected)}'.");
            }
        }
    }

    /// <summary>Asserts human review was routed through the gate.</summary>
    public static void RoutedToHumanReview(RecordingApprovalGate gate)
    {
        if (gate.WasNeverConsulted)
            throw new AgentFlowAssertionException("Expected human-review routing but the approval gate was never consulted.");
    }

    /// <summary>Asserts human review was NOT routed (a human cannot approve away hard evidence).</summary>
    public static void DidNotRouteToHumanReview(RecordingApprovalGate gate)
    {
        if (!gate.WasNeverConsulted)
        {
            throw new AgentFlowAssertionException(
                "Expected no human-review routing but the gate was asked: "
                + string.Join(" | ", gate.Requests.Select(r => Truncate(r.Description))));
        }
    }

    private static AcceptanceResult LastVerdict(FakeDeploymentTarget target)
    {
        if (target.Verdicts.Count == 0)
            throw new AgentFlowAssertionException("No acceptance verdict was reached — the deploy flow never got that far.");
        return target.Verdicts[^1];
    }

    private static string FeedbackStory(FakeArtifactDrafter drafter) =>
        "Feedback per attempt: ["
        + string.Join(
            " -> ",
            drafter.FeedbackPerAttempt.Select((f, i) =>
                $"#{i + 1}:{(f.Count == 0 ? "none" : string.Join(",", f.Select(Truncate)))}"))
        + "]";

    private static string Truncate(string s) =>
        string.IsNullOrEmpty(s) ? "<empty>" : (s.Length <= 60 ? s : s[..60] + "…");
}
