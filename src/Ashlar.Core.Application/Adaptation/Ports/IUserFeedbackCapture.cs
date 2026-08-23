namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Captures user feedback for suggestions (e.g. approve/reject fix).
/// </summary>
public interface IUserFeedbackCapture
{
    /// <summary>
    /// Ask user to approve a suggestion. Returns true if approved, false if rejected.
    /// </summary>
    Task<bool> ApproveAsync(string suggestion, CancellationToken cancellationToken = default);
}
