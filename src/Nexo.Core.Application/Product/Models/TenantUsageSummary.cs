namespace Nexo.Core.Application.Product.Models;

/// <summary>Aggregated usage for one tenant over a rolling window (Product Fleet Phase 0.3).</summary>
public sealed record TenantUsageSummary(
    string TenantId,
    int WindowHours,
    int JobsSubmitted,
    int JobsSucceeded,
    int JobsFailed);
