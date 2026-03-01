using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// Stub capability requester. MVP: returns null (no peer fulfillment).
/// </summary>
public sealed class StubCapabilityRequester : ICapabilityRequester
{
    public Task<Artifact?> RequestAsync(string capability, ArtifactFormat preferredFormat, CancellationToken cancellationToken = default) =>
        Task.FromResult<Artifact?>(null);
}
