using System.Net.Sockets;
using System.Text.Json;
using Nexo.Abstractions.HostBridge;

namespace Nexo.HostBridge;

/// <summary>
/// TCP client that speaks the Nexo NDJSON host-bridge protocol:
/// request <c>{id,token,cmd,args}</c> → response <c>{ok,data|error}</c>.
/// </summary>
public sealed class TcpNdjsonHostBridgeClient : IHostBridgeClient
{
    private readonly HostBridgeEndpoint _endpoint;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    /// <summary>Creates a client for the given endpoint.</summary>
    public TcpNdjsonHostBridgeClient(HostBridgeEndpoint endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    /// <inheritdoc />
    public bool IsConnected
        => _client is { Connected: true } &&
           _reader is not null &&
           _writer is not null;

    /// <inheritdoc />
    public async Task ConnectAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return;

        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(
                        _endpoint.Host,
                        _endpoint.Port,
                        cancellationToken)
                    .ConfigureAwait(false);
                var stream = _client.GetStream();
                _reader = new StreamReader(stream);
                _writer = new StreamWriter(stream) { AutoFlush = true };
                return;
            }
            catch (Exception exception)
            {
                last = exception;
                await DisposeTransportAsync().ConfigureAwait(false);
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException(
            $"Host bridge did not open on {_endpoint.Host}:{_endpoint.Port}.",
            last);
    }

    /// <inheritdoc />
    public async Task<JsonElement> SendAsync(
        string command,
        object? arguments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command is required.", nameof(command));
        if (_reader is null || _writer is null)
            throw new InvalidOperationException("Host bridge client is not connected.");

        var id = Guid.NewGuid().ToString("N");
        var json = JsonSerializer.Serialize(new
        {
            id,
            token = _endpoint.Token,
            cmd = command,
            args = arguments ?? new { }
        });
        await _writer.WriteLineAsync(json).ConfigureAwait(false);
        var line = await _reader.ReadLineAsync(cancellationToken)
            .ConfigureAwait(false);
        if (line is null)
            throw new IOException("Host bridge disconnected.");

        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        if (!root.TryGetProperty("ok", out var okElement) ||
            okElement.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException(
                root.TryGetProperty("error", out var error)
                    ? error.GetString() ?? "Host bridge command failed."
                    : "Host bridge command failed.");
        }

        if (!root.TryGetProperty("data", out var data))
            return default;

        return data.Clone();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeTransportAsync().ConfigureAwait(false);
    }

    private async ValueTask DisposeTransportAsync()
    {
        if (_writer is not null)
            await _writer.DisposeAsync().ConfigureAwait(false);
        _reader?.Dispose();
        _client?.Dispose();
        _writer = null;
        _reader = null;
        _client = null;
    }
}
