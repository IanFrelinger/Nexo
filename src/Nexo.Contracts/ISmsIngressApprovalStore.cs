namespace Nexo.Contracts;

/// <summary>Durable or in-memory idempotent recording of SMS-derived YES approvals.</summary>
public interface ISmsIngressApprovalStore
{
    Task<SmsInboundSimulationResponse> TryRecordApprovalAsync(
        string from,
        string approvalToken,
        string? messageSid,
        CancellationToken cancellationToken);
}
