namespace Ashlar.BackgroundAgents.Extending;

/// <summary>
/// Host-agnostic result of a self-extend run (for background agent use).
///
/// <para><c>Iterations</c> and <c>StoppedReason</c> are populated by the multi-turn
/// ReAct adapter; legacy single-turn callers can leave them at their defaults.</para>
/// </summary>
public record SelfExtendRunResult(
    bool Success,
    int ToolCallsExecuted,
    int ToolCallsDenied,
    string Summary,
    int Iterations = 0,
    string? StoppedReason = null,
    string? GateOutcome = null) : BackgroundAgentRunResult(Success, Summary);
// GateOutcome: when the cycle ran inside an ashlar project, the admission gate's one-line
// verdict ("GATE: held as ext-…"); null outside ashlar projects. Additive with a default so
// every existing construction site and pattern-match keeps compiling.
