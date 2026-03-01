namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Local IPC transport (named pipes or Unix domain sockets).
/// </summary>
public interface ILocalTransport
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task SendAsync(string peerId, byte[] message, CancellationToken cancellationToken = default);
    Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken = default);
}
