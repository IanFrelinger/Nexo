using Nexo.Core.Application.Adaptation.Ports;

namespace Nexo.Infrastructure.Adaptation;

/// <summary>
/// Non-interactive user feedback capture that auto-approves all prompts.
/// Used for CI, tests, and --yes mode.
/// </summary>
public sealed class AutoApproveUserFeedbackCapture : IUserFeedbackCapture
{
    /// <inheritdoc />
    public Task<bool> ApproveAsync(string suggestion, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
