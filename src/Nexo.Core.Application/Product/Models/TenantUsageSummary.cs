namespace Nexo.Core.Application.Product.Models;

/// <summary>Aggregated usage for one tenant over a rolling window (Product Fleet Phase 0.3).</summary>
/// <param name="TenantId">Tenant identifier.</param>
/// <param name="WindowHours">Rolling window length in hours.</param>
/// <param name="JobsSubmitted">Total jobs submitted in the window.</param>
/// <param name="JobsSucceeded">Jobs that completed successfully.</param>
/// <param name="JobsFailed">Jobs that failed or were rejected.</param>
public sealed record TenantUsageSummary(
    string TenantId,
    int WindowHours,
    int JobsSubmitted,
    int JobsSucceeded,
    int JobsFailed);
