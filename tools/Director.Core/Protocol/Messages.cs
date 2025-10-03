using System.Text.Json;

namespace Director.Core.Protocol;

/// <summary>
/// Represents a command sent from client to Unity Editor
/// </summary>
/// <param name="Id">Unique identifier for the command</param>
/// <param name="Type">Command type (see CommandTypes)</param>
/// <param name="Payload">Command-specific payload data</param>
public record DirectorCommand(string Id, string Type, object Payload);

/// <summary>
/// Represents an event sent from Unity Editor to client
/// </summary>
/// <param name="Type">Event type</param>
/// <param name="Payload">Event-specific payload data</param>
public record DirectorEvent(string Type, object Payload);

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
