using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Nexo.Abstractions.HostBridge;
using Nexo.Core.Domain.HostBridge;
using Nexo.HostBridge;
using Xunit;

namespace Nexo.Tests.HostBridge;

public sealed class TcpNdjsonHostBridgeClientTests
{
    [Fact]
    public async Task SendAsync_RoundTripsSuccessfulCommand()
    {
        await using var server = await FakeHostBridgeServer.StartAsync(
            token: "secret",
            handler: static (cmd, _) =>
                cmd == "ping"
                    ? """{"ok":true,"data":{"pong":true}}"""
                    : """{"ok":false,"error":"unknown"}""");

        await using var client = new TcpNdjsonHostBridgeClient(
            HostBridgeEndpoint.Localhost(server.Port, "secret"));
        await client.ConnectAsync(TimeSpan.FromSeconds(5));

        var data = await client.SendAsync("ping", new { });
        Assert.True(data.GetProperty("pong").GetBoolean());
    }

    [Fact]
    public async Task SendAsync_ThrowsWhenHostReturnsError()
    {
        await using var server = await FakeHostBridgeServer.StartAsync(
            token: "secret",
            handler: static (_, _) => """{"ok":false,"error":"denied"}""");

        await using var client = new TcpNdjsonHostBridgeClient(
            HostBridgeEndpoint.Localhost(server.Port, "secret"));
        await client.ConnectAsync(TimeSpan.FromSeconds(5));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendAsync("status"));
        Assert.Equal("denied", exception.Message);
    }

    [Fact]
    public async Task SendAsync_RejectsWrongTokenOnServerSide()
    {
        await using var server = await FakeHostBridgeServer.StartAsync(
            token: "expected",
            handler: static (_, _) => """{"ok":true,"data":{}}""",
            enforceToken: true);

        await using var client = new TcpNdjsonHostBridgeClient(
            HostBridgeEndpoint.Localhost(server.Port, "wrong"));
        await client.ConnectAsync(TimeSpan.FromSeconds(5));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SendAsync("status"));
        Assert.Contains("token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ArtifactRootPath_RejectsEscape()
    {
        var root = Path.Combine(Path.GetTempPath(), "nexo-artifact-" + Guid.NewGuid().ToString("N"));
        try
        {
            var artifactRoot = new ArtifactRootPath(root);
            Assert.Throws<InvalidOperationException>(
                () => artifactRoot.Resolve("../outside.txt"));
            var inside = artifactRoot.Resolve("videos/clip.mp4");
            Assert.StartsWith(artifactRoot.FullPath, inside, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void HostBridgeToken_RequiresValue()
    {
        Assert.Throws<ArgumentException>(() => new HostBridgeToken(" "));
        var token = HostBridgeToken.CreateRandom();
        Assert.False(string.IsNullOrWhiteSpace(token.Value));
    }

    private sealed class FakeHostBridgeServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        private FakeHostBridgeServer(
            TcpListener listener,
            Func<string, JsonElement, string> handler,
            string token,
            bool enforceToken)
        {
            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _loop = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await _listener.AcceptTcpClientAsync(_cts.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    _ = Task.Run(async () =>
                    {
                        using var _ = client;
                        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
                        await using var writer = new StreamWriter(client.GetStream(), Encoding.UTF8)
                        {
                            AutoFlush = true
                        };
                        while (!_cts.IsCancellationRequested)
                        {
                            var line = await reader.ReadLineAsync(_cts.Token)
                                .ConfigureAwait(false);
                            if (line is null)
                                break;

                            using var document = JsonDocument.Parse(line);
                            var root = document.RootElement;
                            var id = root.GetProperty("id").GetString() ?? "";
                            var cmd = root.GetProperty("cmd").GetString() ?? "";
                            var requestToken = root.GetProperty("token").GetString() ?? "";
                            var args = root.TryGetProperty("args", out var argsElement)
                                ? argsElement.Clone()
                                : default;

                            string body;
                            if (enforceToken &&
                                !string.Equals(requestToken, token, StringComparison.Ordinal))
                            {
                                body = """{"ok":false,"error":"invalid token"}""";
                            }
                            else
                            {
                                body = handler(cmd, args);
                            }

                            using var responseDoc = JsonDocument.Parse(body);
                            var ok = responseDoc.RootElement.GetProperty("ok").GetBoolean();
                            object payload = ok
                                ? new
                                {
                                    id,
                                    ok = true,
                                    data = responseDoc.RootElement.TryGetProperty(
                                        "data",
                                        out var data)
                                        ? data
                                        : (object)new { }
                                }
                                : new
                                {
                                    id,
                                    ok = false,
                                    error = responseDoc.RootElement.TryGetProperty(
                                        "error",
                                        out var error)
                                        ? error.GetString()
                                        : "error"
                                };
                            await writer.WriteLineAsync(
                                    JsonSerializer.Serialize(payload))
                                .ConfigureAwait(false);
                        }
                    }, _cts.Token);
                }
            }, _cts.Token);
        }

        public int Port { get; }

        public static async Task<FakeHostBridgeServer> StartAsync(
            string token,
            Func<string, JsonElement, string> handler,
            bool enforceToken = false)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var server = new FakeHostBridgeServer(
                listener,
                handler,
                token,
                enforceToken);
            await Task.Yield();
            return server;
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                await _loop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // expected
            }

            _cts.Dispose();
        }
    }
}
