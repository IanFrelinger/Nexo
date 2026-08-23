namespace Ashlar.Contracts;

/// <summary>Outcome of processing a simulated inbound SMS approval message.</summary>
/// <param name="Accepted">Whether the message was accepted and recorded.</param>
/// <param name="ApprovalToken">Token derived from the approval, when <paramref name="Accepted"/> is true.</param>
/// <param name="Message">Human-readable status for operators and API consumers.</param>
/// <param name="IdempotentReplay">True when the store recognized a prior identical approval.</param>
public sealed record SmsInboundSimulationResponse(
    bool Accepted,
    string? ApprovalToken,
    string Message,
    bool IdempotentReplay);
