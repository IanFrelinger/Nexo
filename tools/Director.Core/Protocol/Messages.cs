using System.Text.Json;

namespace Director.Core.Protocol;

/// <summary>
/// Represents a command sent from client to Unity Editor
/// </summary>
public sealed class DirectorCommand
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object Payload { get; set; } = new();

    public DirectorCommand() { }

    public DirectorCommand(string id, string type, object payload)
    {
        Id = id;
        Type = type;
        Payload = payload;
    }
}

/// <summary>
/// Represents an event sent from Unity Editor to client
/// </summary>
public sealed class DirectorEvent
{
    public string Type { get; set; } = string.Empty;
    public object Payload { get; set; } = new();

    public DirectorEvent() { }

    public DirectorEvent(string type, object payload)
    {
        Type = type;
        Payload = payload;
    }
}

/// <summary>
/// Standard log levels for log events
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
