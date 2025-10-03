using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Director.Core.Protocol;

namespace Director.Avalonia.Services;

/// <summary>
/// Mock Unity service that simulates Unity Editor behavior without requiring network ports
/// Uses file-based communication for sandboxed local testing
/// </summary>
public sealed class MockUnityService : IDisposable
{
    private readonly ILogger<MockUnityService> _logger;
    private readonly string _communicationDirectory;
    private readonly Timer _eventTimer;
    private bool _disposed = false;
    private int _eventCounter = 0;

    public MockUnityService(ILogger<MockUnityService> logger)
    {
        _logger = logger;
        _communicationDirectory = Path.Combine(Path.GetTempPath(), "DirectorStudioMockUnity");
        Directory.CreateDirectory(_communicationDirectory);
        
        // Start sending mock events every 2 seconds
        _eventTimer = new Timer(SendMockEvent, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        
        _logger.LogInformation("Mock Unity Service initialized with file-based communication");
    }

    /// <summary>
    /// Simulates Unity Editor startup and sends initial events
    /// </summary>
    public async Task SimulateUnityStartupAsync()
    {
        _logger.LogInformation("Simulating Unity Editor startup...");
        
        var events = new[]
        {
            new DirectorEvent
            {
                Type = "ConnectionEvent",
                Payload = new ConnectionEventPayload
                {
                    Connected = true,
                    Message = "Connected to Mock Unity Editor",
                    UnityVersion = "2022.3.15f1",
                    ProjectName = "NexoDirectorDemo"
                }
            },
                new DirectorEvent
                {
                    Type = "LogLine",
                    Payload = new LogLinePayload
                    {
                        Line = "Unity Editor started successfully",
                        Level = Director.Core.Protocol.LogLevel.Information
                    }
                },
                new DirectorEvent
                {
                    Type = "LogLine",
                    Payload = new LogLinePayload
                    {
                        Line = "Director Server initialized",
                        Level = Director.Core.Protocol.LogLevel.Information
                    }
                },
                new DirectorEvent
                {
                    Type = "LogLine",
                    Payload = new LogLinePayload
                    {
                        Line = "Nexo package loaded",
                        Level = Director.Core.Protocol.LogLevel.Information
                    }
                },
            new DirectorEvent
            {
                Type = "GateResult",
                Payload = new GateResultPayload
                {
                    GateName = "Architecture Validation",
                    Passed = true,
                    Message = "All architectural patterns validated successfully"
                }
            },
            new DirectorEvent
            {
                Type = "GateResult",
                Payload = new GateResultPayload
                {
                    GateName = "Code Quality Check",
                    Passed = true,
                    Message = "Code quality standards met"
                }
            },
            new DirectorEvent
            {
                Type = "GateResult",
                Payload = new GateResultPayload
                {
                    GateName = "Performance Test",
                    Passed = false,
                    Message = "Performance threshold exceeded"
                }
            }
        };

        foreach (var evt in events)
        {
            await WriteEventToFileAsync(evt);
            await Task.Delay(500); // Simulate real-time event delivery
        }

        _logger.LogInformation("Unity Editor startup simulation completed");
    }

    /// <summary>
    /// Sends periodic mock events to simulate ongoing Unity activity
    /// </summary>
    private async void SendMockEvent(object? state)
    {
        if (_disposed) return;

        try
        {
            _eventCounter++;
            
            var events = new[]
            {
                new DirectorEvent
                {
                    Type = "LogLine",
                    Payload = new LogLinePayload
                    {
                        Line = $"Unity Editor update #{_eventCounter}",
                        Level = Director.Core.Protocol.LogLevel.Debug
                    }
                }
            };

            // Occasionally send gate results
            if (_eventCounter % 5 == 0)
            {
                var gateEvents = new[]
                {
                    new DirectorEvent
                    {
                        Type = "GateResult",
                        Payload = new GateResultPayload
                        {
                            GateName = $"Periodic Check #{_eventCounter / 5}",
                            Passed = _eventCounter % 10 != 0, // Fail every 10th check
                            Message = _eventCounter % 10 != 0 
                                ? "Periodic validation passed" 
                                : "Periodic validation failed - threshold exceeded"
                        }
                    }
                };

                foreach (var evt in gateEvents)
                {
                    await WriteEventToFileAsync(evt);
                }
            }

            foreach (var evt in events)
            {
                await WriteEventToFileAsync(evt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mock event");
        }
    }

    /// <summary>
    /// Writes an event to a file for the Avalonia app to read
    /// </summary>
    private async Task WriteEventToFileAsync(DirectorEvent evt)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var fileName = Path.Combine(_communicationDirectory, $"event_{timestamp}.json");
            
            var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(fileName, json);
            
            // Clean up old files (keep only last 50)
            await CleanupOldFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing event to file");
        }
    }

    /// <summary>
    /// Cleans up old event files to prevent disk space issues
    /// </summary>
    private Task CleanupOldFilesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_communicationDirectory, "event_*.json")
                .OrderBy(f => File.GetCreationTime(f))
                .ToArray();

            if (files.Length > 50)
            {
                var filesToDelete = files.Take(files.Length - 50);
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up old event files");
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the communication directory path for the Avalonia app to monitor
    /// </summary>
    public string CommunicationDirectory => _communicationDirectory;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _eventTimer?.Dispose();
            
            try
            {
                if (Directory.Exists(_communicationDirectory))
                {
                    Directory.Delete(_communicationDirectory, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up communication directory");
            }
            
            _logger.LogInformation("Mock Unity Service disposed");
        }
    }
}

// Payload classes for the mock events
public class ConnectionEventPayload
{
    public bool Connected { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UnityVersion { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
}

public class LogLinePayload
{
    public string Line { get; set; } = string.Empty;
    public Director.Core.Protocol.LogLevel Level { get; set; }
}

public class GateResultPayload
{
    public string GateName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}
