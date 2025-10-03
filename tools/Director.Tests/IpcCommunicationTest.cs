using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Director.Core.Protocol;

namespace Director.Tests;

/// <summary>
/// Test class to validate Director Studio IPC communication
/// This class tests the TCP communication between Avalonia and Unity
/// </summary>
public class IpcCommunicationTest
{
    private const int Port = 5088;
    private TcpListener? _server;
    private bool _isRunning = false;
    private readonly List<string> _receivedMessages = new();

    public async Task<bool> RunTestAsync()
    {
        Console.WriteLine("Director Studio IPC Communication Test");
        Console.WriteLine("=====================================");
        
        try
        {
            return await TestMockUnityServerAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestMockUnityServerAsync()
    {
        Console.WriteLine("\n=== Testing Avalonia Connection ===");
        
        if (!await StartMockUnityServerAsync())
        {
            return false;
        }

        try
        {
            Console.WriteLine($"Mock Unity Server started on port {Port}");
            Console.WriteLine("Mock server is ready. You can now:");
            Console.WriteLine("1. Start the Avalonia app: dotnet run --project tools/Director.Avalonia/Director.Avalonia.csproj");
            Console.WriteLine("2. Enter any token in the app");
            Console.WriteLine("3. Click Connect");
            Console.WriteLine("4. Watch for connection and test events");
            
            // Keep server running for a short time to test
            await Task.Delay(5000);
            
            return true;
        }
        finally
        {
            await StopMockServerAsync();
        }
    }

    private async Task<bool> StartMockUnityServerAsync()
    {
        try
        {
            _server = new TcpListener(IPAddress.Loopback, Port);
            _server.Start();
            _isRunning = true;

            // Start server in background
            _ = Task.Run(HandleConnectionsAsync);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start mock server: {ex.Message}");
            return false;
        }
    }

    private async Task HandleConnectionsAsync()
    {
        while (_isRunning && _server != null)
        {
            try
            {
                var client = await _server.AcceptTcpClientAsync();
                Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}");
                
                // Handle client in background
                _ = Task.Run(() => HandleClientAsync(client));
            }
            catch (ObjectDisposedException)
            {
                // Server was stopped
                break;
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"Error handling connection: {ex.Message}");
                }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            // Read authentication token
            var token = await reader.ReadLineAsync();
            Console.WriteLine($"Received token: {token}");

            // Send welcome message
            var welcomeMessage = new DirectorEvent
            {
                Type = "ConnectionEvent",
                Payload = new ConnectionEvent(true, "Connected to Mock Unity Editor")
            };
            await SendMessageAsync(writer, welcomeMessage);

            // Send test events
            await SendTestEventsAsync(writer);

            // Keep connection alive for a bit
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client disconnected");
        }
    }

    private async Task SendMessageAsync(StreamWriter writer, DirectorEvent message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            await writer.WriteLineAsync(json);
            Console.WriteLine($"Sent: {json}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    private async Task SendTestEventsAsync(StreamWriter writer)
    {
        var events = new[]
        {
            new DirectorEvent
            {
                Type = "LogLine",
                Payload = new LogLineEvent("Unity Editor started successfully", LogLevel.Information)
            },
            new DirectorEvent
            {
                Type = "LogLine",
                Payload = new LogLineEvent("Director Server initialized", LogLevel.Information)
            },
            new DirectorEvent
            {
                Type = "GateResult",
                Payload = new GateResultEvent("Architecture Validation", true, "All architecture rules passed")
            },
            new DirectorEvent
            {
                Type = "PlayStateEvent",
                Payload = new PlayStateEvent(false)
            }
        };

        foreach (var evt in events)
        {
            await SendMessageAsync(writer, evt);
            await Task.Delay(500);
        }
    }

    private async Task StopMockServerAsync()
    {
        _isRunning = false;
        _server?.Stop();
        Console.WriteLine("Mock Unity Server stopped");
    }

    // Removed Main method - using TestRunner instead
}
