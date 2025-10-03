using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Director.Core.Protocol;

namespace Director.Avalonia.Services;

public sealed class DirectorClient : IDisposable
{
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isConnected = false;

    public event EventHandler<DirectorEvent>? EventReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected => _isConnected;

    public async Task<bool> ConnectAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(IPAddress.Loopback, 5088, cancellationToken);
            
            var stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            // Send authentication token
            await _writer.WriteLineAsync(token);

            // Start receiving loop
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(ReceiveLoopAsync, _cancellationTokenSource.Token);

            _isConnected = true;
            ConnectionStatusChanged?.Invoke(this, "Connected");
            return true;
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _isConnected = false;
        _cancellationTokenSource?.Cancel();
        
        _writer?.Dispose();
        _reader?.Dispose();
        _tcpClient?.Dispose();
        
        await Task.Delay(10); // Small delay to prevent warning
        ConnectionStatusChanged?.Invoke(this, "Disconnected");
    }

    public async Task SendCommandAsync(DirectorCommand command)
    {
        if (!_isConnected || _writer == null) return;

        try
        {
            var json = JsonSerializer.Serialize(command);
            await _writer.WriteLineAsync(json);
        }
        catch (Exception ex)
        {
            ConnectionStatusChanged?.Invoke(this, $"Send failed: {ex.Message}");
        }
    }

    private async Task ReceiveLoopAsync()
    {
        while (_isConnected && _reader != null && !_cancellationTokenSource!.Token.IsCancellationRequested)
        {
            try
            {
                var line = await _reader.ReadLineAsync();
                if (line == null) break;

                var evt = JsonSerializer.Deserialize<DirectorEvent>(line);
                if (evt != null)
                {
                    EventReceived?.Invoke(this, evt);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Receive error: {ex.Message}");
                break;
            }
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _writer?.Dispose();
        _reader?.Dispose();
        _tcpClient?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
