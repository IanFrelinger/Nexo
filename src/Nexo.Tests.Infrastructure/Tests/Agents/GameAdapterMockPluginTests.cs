using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Infrastructure.Tests.Agents;

public sealed class GameAdapterMockPluginTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await CanTalkToMockPluginAndPerformBasicCalls(cancellationToken);

            return new TestResult
            {
                Name = nameof(GameAdapterMockPluginTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "GameAdapter mock plugin test passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(GameAdapterMockPluginTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(GameAdapterMockPluginTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task CanTalkToMockPluginAndPerformBasicCalls(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var (listener, port) = await MockGamePlugin.StartAsync(cts.Token);

        await using var adapter = new GameAdapter();
        await adapter.ConnectAsync($"tcp://127.0.0.1:{port}", cts.Token);

        var screenshot = await adapter.CaptureScreenshotAsync(cts.Token);
        AssertNotNull(screenshot);
        AssertTrue(screenshot!.Length > 10);

        var gameState = await adapter.GetGameStateAsync(cts.Token);
        AssertNotNull(gameState);

        var playerState = await adapter.GetPlayerStateAsync(cts.Token);
        AssertNotNull(playerState);

        var interactables = await adapter.GetInteractiveElementsAsync(cts.Token);
        AssertNotNull(interactables);

        var result = await adapter.ExecuteActionAsync(new TestAction
        {
            Id = "a1",
            Type = ActionType.Interact,
            Description = "Interact with mock door",
            ElementId = "mock-door-1"
        }, cts.Token);

        AssertNotNull(result);

        await adapter.DisconnectAsync(cts.Token);
        listener.Stop();
    }

    private static class MockGamePlugin
    {
        public static Task<(System.Net.Sockets.TcpListener listener, int port)> StartAsync(CancellationToken ct)
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct);
                    _ = HandleClientAsync(client, ct);
                }
            }, ct);

            return Task.FromResult<(System.Net.Sockets.TcpListener listener, int port)>((listener, port));
        }

        private static async Task HandleClientAsync(System.Net.Sockets.TcpClient client, CancellationToken ct)
        {
            try
            {
                using var __ = client;
                var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                while (!ct.IsCancellationRequested && client.Connected)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line == null) break;

                    var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var cmd = parts.Length > 0 ? parts[0].Trim().ToUpperInvariant() : "";
                    var arg = parts.Length > 1 ? parts[1] : "";

                    switch (cmd)
                    {
                        case "HELLO":
                            await writer.WriteLineAsync("NEXO_PLUGIN 1.0");
                            break;
                        case "SCREENSHOT":
                            await writer.WriteLineAsync("DATA:" + Convert.ToBase64String(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 }));
                            break;
                        case "GAMESTATE":
                            await writer.WriteLineAsync("{\"SceneName\":\"MockScene\"}");
                            break;
                        case "PLAYERSTATE":
                            await writer.WriteLineAsync("{\"Health\":100}");
                            break;
                        case "INTERACTABLES":
                            await writer.WriteLineAsync("[{\"Id\":\"mock-door-1\",\"Type\":\"Door\",\"Label\":\"Mock Door\",\"IsVisible\":true,\"IsEnabled\":true,\"PossibleActions\":[\"interact\"]}]");
                            break;
                        case "INTERACT":
                            await writer.WriteLineAsync($"OK INTERACT {arg}");
                            break;
                        default:
                            await writer.WriteLineAsync("OK");
                            break;
                    }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}

