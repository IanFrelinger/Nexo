using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Director.Core.Protocol;
using Microsoft.Extensions.Logging;

namespace Director.Avalonia.Services;

public sealed class DirectorClient : IDisposable
{
    private readonly ILogger<DirectorClient> _logger;
    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cancellationTokenSource;
    private FileBasedEventReader? _fileEventReader;
    private MockUnityService? _mockUnityService;
    private bool _isConnected = false;
    private bool _useFileBasedCommunication = false;

    public event EventHandler<DirectorEvent>? EventReceived;
    public event EventHandler<string>? ConnectionStatusChanged;

    public bool IsConnected => _isConnected;
    public bool UseFileBasedCommunication 
    { 
        get => _useFileBasedCommunication; 
        set => _useFileBasedCommunication = value; 
    }

    public DirectorClient(ILogger<DirectorClient> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_useFileBasedCommunication)
            {
                return await ConnectFileBasedAsync(token, cancellationToken);
            }
            else
            {
                return await ConnectTcpAsync(token, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection failed");
            ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ConnectTcpAsync(string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting via TCP to Unity Director Server...");
        
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
        ConnectionStatusChanged?.Invoke(this, "Connected via TCP");
        _logger.LogInformation("Successfully connected via TCP");
        return true;
    }

    private async Task<bool> ConnectFileBasedAsync(string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connecting via file-based communication...");
        
        // Create mock Unity service
        var mockLogger = _logger as ILogger<MockUnityService> ?? 
            new LoggerFactory().CreateLogger<MockUnityService>();
        _mockUnityService = new MockUnityService(mockLogger);
        
        // Create file event reader
        var fileLogger = _logger as ILogger<FileBasedEventReader> ?? 
            new LoggerFactory().CreateLogger<FileBasedEventReader>();
        _fileEventReader = new FileBasedEventReader(fileLogger, _mockUnityService.CommunicationDirectory);
        _fileEventReader.EventReceived += OnFileEventReceived;
        
        // Start the mock Unity service
        await _mockUnityService.SimulateUnityStartupAsync();
        
        // Start reading events
        await _fileEventReader.StartReadingAsync();

        _isConnected = true;
        ConnectionStatusChanged?.Invoke(this, "Connected via file-based communication");
        _logger.LogInformation("Successfully connected via file-based communication");
        return true;
    }

    private void OnFileEventReceived(object? sender, DirectorEvent evt)
    {
        EventReceived?.Invoke(this, evt);
    }

    public async Task DisconnectAsync()
    {
        _isConnected = false;
        _cancellationTokenSource?.Cancel();
        
        if (_useFileBasedCommunication)
        {
            _fileEventReader?.Dispose();
            _mockUnityService?.Dispose();
        }
        else
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _tcpClient?.Dispose();
        }
        
        await Task.Delay(10); // Small delay to prevent warning
        ConnectionStatusChanged?.Invoke(this, "Disconnected");
        _logger.LogInformation("Disconnected from Unity");
    }

    public async Task SendCommandAsync(DirectorCommand command)
    {
        if (!_isConnected) return;

        try
        {
            if (_useFileBasedCommunication)
            {
                // For file-based communication, we can't send commands back to Unity
                // This would require a separate command file mechanism
                _logger.LogWarning("Command sending not supported in file-based communication mode");
                ConnectionStatusChanged?.Invoke(this, "Command sending not supported in file-based mode");
            }
            else if (_writer != null)
            {
                var json = JsonSerializer.Serialize(command);
                await _writer.WriteLineAsync(json);
                _logger.LogDebug($"Sent command: {command.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command");
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
        
        if (_useFileBasedCommunication)
        {
            _fileEventReader?.Dispose();
            _mockUnityService?.Dispose();
        }
        else
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _tcpClient?.Dispose();
        }
        
        _cancellationTokenSource?.Dispose();
        _logger.LogInformation("DirectorClient disposed");
    }
}
