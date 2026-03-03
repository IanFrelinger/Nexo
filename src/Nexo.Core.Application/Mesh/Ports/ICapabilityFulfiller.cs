using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Fulfills capability requests from peers. Registers handlers and processes incoming requests.
/// Phase E: Local IPC mesh - one instance fulfills requests from another.
/// </summary>
public interface ICapabilityFulfiller
{
    /// <summary>
    /// Registers a handler for a capability. When a request arrives, the handler produces the artifact.
    /// </summary>
    void RegisterHandler(string capability, Func<ArtifactFormat, CancellationToken, Task<Artifact>> handler);

    /// <summary>
    /// Processes one incoming request from the inbox, if any. Returns true if a request was processed.
    /// </summary>
    Task<bool> ProcessOneAsync(CancellationToken cancellationToken = default);
}
