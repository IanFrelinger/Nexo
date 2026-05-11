using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

public interface ISmsIngressApprovalStore
{
    Task<SmsInboundSimulationResponse> TryRecordApprovalAsync(
        string from,
        string approvalToken,
        string? messageSid,
        CancellationToken cancellationToken);
}
