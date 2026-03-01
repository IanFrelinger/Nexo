using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// Stub local transport. MVP: no-op for same-machine file-based discovery.
/// </summary>
public sealed class StubLocalTransport : ILocalTransport
{
    public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendAsync(string peerId, byte[] message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken = default) => Task.FromResult<byte[]?>(null);
}
