namespace Nexo.Contracts;

/// <summary>
/// Default <see cref="ISmsIngressApprovalStore"/> for hosts that do not configure SMS ingress (e.g. CLI).
/// </summary>
public sealed class UnsupportedSmsIngressApprovalStore : ISmsIngressApprovalStore
{
    /// <inheritdoc />
    public Task<SmsInboundSimulationResponse> TryRecordApprovalAsync(
        string from,
        string approvalToken,
        string? messageSid,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            new SmsInboundSimulationResponse(false, null, "SMS ingress approval store is not configured for this host.", false));
}
