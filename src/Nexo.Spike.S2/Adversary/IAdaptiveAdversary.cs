using Nexo.Spike.S0.Models;
using Nexo.Spike.S0.Roles;
using Nexo.Spike.S2.Oracle;

namespace Nexo.Spike.S2.Adversary;

public enum AdaptiveOutcomeKind
{
    Rejected,
    TrueEscape,
    BenignPass
}

public sealed record AdaptiveCandidate(
    string CandidateId,
    string ImplementationSource,
    string Hypothesis);

public sealed record GateVerdict(
    int AttemptIndex,
    string CandidateId,
    AdaptiveOutcomeKind Outcome,
    string? RejectedByGate,
    string? RejectionDetail,
    IReadOnlyList<string>? GatesPassed,
    IReadOnlyList<OracleDisagreement>? OracleDisagreements);

public sealed record AdaptiveAttemptContext(
    string IntentId,
    BrickSpec VisibleSpec,
    RoleView ImplementerRole,
    IReadOnlyList<GateVerdict> PriorVerdicts,
    int AttemptIndex,
    int MaxAttempts);

public interface IAdaptiveAdversary
{
    string BackendName { get; }

    AdaptiveCandidate GenerateNext(AdaptiveAttemptContext context);
}
