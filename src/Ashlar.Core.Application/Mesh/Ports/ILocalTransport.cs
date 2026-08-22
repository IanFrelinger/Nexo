namespace Ashlar.Core.Application.Mesh.Ports;

/// <summary>
/// Local IPC transport (named pipes or Unix domain sockets).
/// </summary>
public interface ILocalTransport
{
    /// <summary>Establishes a connection to the local transport endpoint.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Sends a message to the specified peer.</summary>
    /// <param name="peerId">Destination peer identifier.</param>
    /// <param name="message">Serialized message bytes.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SendAsync(string peerId, byte[] message, CancellationToken cancellationToken = default);

    /// <summary>Receives the next message, if any.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Message bytes, or null when no message is available.</returns>
    Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken = default);
}
