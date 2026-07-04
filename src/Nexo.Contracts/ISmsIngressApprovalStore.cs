namespace Nexo.Contracts;

/// <summary>Durable or in-memory idempotent recording of SMS-derived YES approvals.</summary>
public interface ISmsIngressApprovalStore
{
    /// <summary>
    /// Attempts to record a YES approval idempotently.
    /// Returns a replay response when the same sender and idempotency key were seen before.
    /// </summary>
    /// <param name="from">Sender phone number or identifier.</param>
    /// <param name="approvalToken">Derived approval token from the inbound message.</param>
    /// <param name="messageSid">Optional provider message id for idempotency.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    Task<SmsInboundSimulationResponse> TryRecordApprovalAsync(
        string from,
        string approvalToken,
        string? messageSid,
        CancellationToken cancellationToken);
}
