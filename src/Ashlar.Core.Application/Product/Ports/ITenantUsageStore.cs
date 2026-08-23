using Ashlar.Core.Application.Product.Models;

namespace Ashlar.Core.Application.Product.Ports;

/// <summary>
/// In-process usage counters per tenant (jobs submitted/succeeded). Phase 0.3 foundation for metering and billing hooks.
/// </summary>
public interface ITenantUsageStore
{
    void RecordJobSubmitted(string tenantId);

    void RecordJobCompleted(string tenantId, bool success);

    TenantUsageSummary GetSummary(string tenantId, int windowHours);
}
