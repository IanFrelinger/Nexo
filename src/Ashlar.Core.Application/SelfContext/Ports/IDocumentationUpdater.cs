namespace Ashlar.Core.Application.SelfContext.Ports;

/// <summary>
/// Updates documentation when brick behavior changes via adaptation.
/// Phase F: documentation loop.
/// </summary>
public interface IDocumentationUpdater
{
    Task UpdateForAdaptationAsync(string adaptationId, CancellationToken ct = default);
    Task GenerateStubAsync(string componentId, CancellationToken ct = default);
}
