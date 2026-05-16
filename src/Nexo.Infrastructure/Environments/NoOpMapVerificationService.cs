using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;

namespace Nexo.Infrastructure.Environments;

/// <summary>
/// Pass-through verification (always passes). Replace with geometry/topology validators or AI-assisted checks.
/// </summary>
public sealed class NoOpMapVerificationService : IMapVerificationService
{
    public Task<MapVerificationReport> VerifyAsync(
        MapVerificationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MapVerificationReport(request.TierIndex, Array.Empty<MapVerificationIssue>(), PassedCoreChecks: true));
}
