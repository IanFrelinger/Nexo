using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Abstractions.HostBridge;

/// <summary>
/// Client for a localhost (or LAN) NDJSON host bridge used by agents and runners.
/// </summary>
public interface IHostBridgeClient : IAsyncDisposable
{
    /// <summary>Whether the underlying transport is connected.</summary>
    bool IsConnected { get; }

    /// <summary>Connects to the host bridge, retrying until <paramref name="timeout"/> elapses.</summary>
    Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>Sends one command and returns the successful <c>data</c> payload.</summary>
    /// <exception cref="InvalidOperationException">When the host returns <c>ok: false</c>.</exception>
    /// <exception cref="IOException">When the transport disconnects.</exception>
    Task<JsonElement> SendAsync(
        string command,
        object? arguments = null,
        CancellationToken cancellationToken = default);
}
