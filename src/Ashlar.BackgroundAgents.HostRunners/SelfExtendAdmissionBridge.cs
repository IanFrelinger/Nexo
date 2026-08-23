using Microsoft.Extensions.Logging;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// The producer wiring: when a self-extend cycle runs inside an ashlar project (a repo with
/// <c>ashlar.policy.yaml</c> at its root), the cycle's outcome is recorded as an
/// <see cref="ExtensionProposal"/> through the admission gate — the same store
/// <c>ashlar gates</c> reads, so a cycle's work shows up in the held queue for a person to
/// seat or refuse.
///
/// <para>Outside an ashlar project this is a no-op: the runner behaves exactly as before.
/// The courses attached to the proposal are ONLY what the cycle actually evidences — today
/// that is the <c>sandbox</c> course (from the policy engine's denial count). A project
/// whose policy requires more gates (tests, security) will therefore see the proposal
/// REJECTED with "gate did not run" — which is correct and fail-closed: the runtime may not
/// claim courses it did not run, and the operator's policy decides whether the evidence
/// suffices.</para>
///
/// <para>Honest scope note: v0 records AFTER the cycle's mediated writes, making the gate a
/// ledger of what happened plus the human queue for it. Moving admission BEFORE the write
/// lands (propose → hold → apply) is the M1 enforcement ordering, tracked in SPEC-004.</para>
/// </summary>
public static class SelfExtendAdmissionBridge
{
    /// <summary>
    /// Records the cycle as a proposal when running inside an ashlar project.
    /// </summary>
    /// <returns>A one-line gate outcome for the run summary, or null when not an ashlar
    /// project or nothing was written.</returns>
    public static async Task<string?> TryRecordAsync(
        string repoRoot,
        string agentName,
        string? objective,
        IReadOnlyList<string> writePaths,
        int toolCallsExecuted,
        int toolCallsDenied,
        ILogger logger,
        CancellationToken ct = default)
    {
        var policyPath = Path.Combine(repoRoot, "ashlar.policy.yaml");
        if (!File.Exists(policyPath))
        {
            return null;   // not an ashlar project; the runner is unchanged
        }
        if (writePaths.Count == 0)
        {
            return null;   // nothing to propose
        }

        string policyYaml;
        try
        {
            policyYaml = await File.ReadAllTextAsync(policyPath, ct).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Self-extend gate: could not read ashlar.policy.yaml");
            return $"GATE ERROR: could not read ashlar.policy.yaml ({ex.Message})";
        }

        if (!PolicyLoader.TryLoad(policyYaml, out var policy, out var reason))
        {
            // Fail loud, never silent: an unreadable envelope is an error the operator must
            // see, not a skipped gate.
            logger.LogError("Self-extend gate: policy rejected: {Reason}", reason);
            return $"GATE ERROR: {reason}";
        }

        var proposal = BuildProposal(agentName, objective, writePaths, toolCallsExecuted, toolCallsDenied);
        var store = new GateStore(Path.Combine(repoRoot, ".ashlar"));
        var record = await store.ProposeAsync(policy!, proposal, DateTimeOffset.UtcNow, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Self-extend gate: proposal {Id} -> {State} ({Reason})",
            proposal.Id, record.State, record.Reason);

        return record.State switch
        {
            ProposalState.Held => $"GATE: held as {proposal.Id} — review with `ashlar gates`",
            ProposalState.Admitted => $"GATE: admitted as {proposal.Id} — {record.Reason}",
            _ => $"GATE: rejected — {record.Reason}",
        };
    }

    /// <summary>
    /// Maps cycle facts onto a proposal. Public and pure so tests pin the mapping without a
    /// filesystem. The courses claim ONLY what the cycle evidences: the sandbox course from
    /// the policy engine's own denial count.
    /// </summary>
    public static ExtensionProposal BuildProposal(
        string agentName,
        string? objective,
        IReadOnlyList<string> writePaths,
        int toolCallsExecuted,
        int toolCallsDenied)
    {
        var confined = toolCallsDenied == 0;
        return new ExtensionProposal
        {
            // Matches the store's id allowlist: alphanumeric start, [A-Za-z0-9_-].
            Id = "ext-" + Guid.NewGuid().ToString("N")[..12],
            Kind = "brick",
            Summary = string.IsNullOrWhiteSpace(objective)
                ? $"self-extend cycle by {agentName}: {writePaths.Count} file(s) changed"
                : Truncate(objective!, 120),
            ProposedBy = agentName,
            ProposedAt = DateTimeOffset.UtcNow,
            Diff = string.Join("\n", writePaths.Select(p => "~ " + p)),
            Courses =
            [
                new CourseResult
                {
                    Name = "sandbox",
                    Passed = confined,
                    Detail = confined
                        ? $"{toolCallsExecuted} tool call(s), 0 denied"
                        : $"{toolCallsDenied} tool call(s) DENIED by the policy engine",
                },
            ],
        };
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}
