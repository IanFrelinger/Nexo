namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Tracks core version and records changes.
/// </summary>
public interface ICoreVersionManager
{
    /// <summary>Returns the current core version string.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>Records a core change with a human-readable description.</summary>
    /// <param name="changeDescription">Description of the change applied.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RecordChangeAsync(string changeDescription, CancellationToken cancellationToken = default);
}
