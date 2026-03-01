namespace Nexo.Core.Application.Adaptation.Ports;

/// <summary>
/// Tracks core version and records changes.
/// </summary>
public interface ICoreVersionManager
{
    Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default);
    Task RecordChangeAsync(string changeDescription, CancellationToken cancellationToken = default);
}
