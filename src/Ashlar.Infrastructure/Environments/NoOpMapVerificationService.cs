using Ashlar.Core.Application.Environments;
using Ashlar.Core.Application.Environments.Ports;

namespace Ashlar.Infrastructure.Environments;

/// <summary>
/// Pass-through verification (always passes). Replace with geometry/topology validators or AI-assisted checks.
/// </summary>
public sealed class NoOpMapVerificationService : IMapVerificationService
{
    /// <summary>Verify asynchronously.</summary>
    public Task<MapVerificationReport> VerifyAsync(
        MapVerificationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MapVerificationReport(request.TierIndex, Array.Empty<MapVerificationIssue>(), PassedCoreChecks: true));
}
