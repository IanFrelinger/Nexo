using System.Collections.Concurrent;
using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>In-memory idempotent approvals for SMS simulation labs only.</summary>
public sealed class MemorySmsIngressApprovalStore : ISmsIngressApprovalStore
{
    private readonly ConcurrentDictionary<string, SmsInboundSimulationResponse> _byExternalId = new(StringComparer.Ordinal);

    public Task<SmsInboundSimulationResponse> TryRecordApprovalAsync(
        string from,
        string approvalToken,
        string? messageSid,
        CancellationToken cancellationToken)
    {
        var externalId = SmsIngressExternalIds.Build(from, approvalToken, messageSid);

        if (_byExternalId.TryGetValue(externalId, out var existing))
            return Task.FromResult(existing with { IdempotentReplay = true });

        var fresh = new SmsInboundSimulationResponse(true, approvalToken, "Recorded YES approval.", false);
        _byExternalId[externalId] = fresh;
        return Task.FromResult(fresh);
    }
}
