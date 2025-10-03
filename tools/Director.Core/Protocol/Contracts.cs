using System.Text.Json;

namespace Director.Core.Protocol;

/// <summary>
/// Payload for running Nexo CLI commands
/// </summary>
public sealed class RunNexoPayload
{
    public string Command { get; set; } = string.Empty;
    public string[] Args { get; set; } = Array.Empty<string>();
    public string? WorkingDirectory { get; set; }

    public RunNexoPayload() { }

    public RunNexoPayload(string command, string[] args, string? workingDirectory = null)
    {
        Command = command;
        Args = args;
        WorkingDirectory = workingDirectory;
    }
}

/// <summary>
/// Payload for opening Unity scenes
/// </summary>
public sealed class OpenScenePayload
{
    public string ScenePath { get; set; } = string.Empty;

    public OpenScenePayload() { }

    public OpenScenePayload(string scenePath)
    {
        ScenePath = scenePath;
    }
}

/// <summary>
/// Payload for toggling Unity play mode
/// </summary>
public sealed class TogglePlayPayload
{
    public bool? ForceState { get; set; }

    public TogglePlayPayload() { }

    public TogglePlayPayload(bool? forceState = null)
    {
        ForceState = forceState;
    }
}

/// <summary>
/// Payload for applying UI modifications to Unity Editor
/// </summary>
public sealed class ApplyUIModPayload
{
    public string TargetSlot { get; set; } = string.Empty;
    public JsonElement UiSchema { get; set; }

    public ApplyUIModPayload() { }

    public ApplyUIModPayload(string targetSlot, JsonElement uiSchema)
    {
        TargetSlot = targetSlot;
        UiSchema = uiSchema;
    }
}

/// <summary>
/// Payload for getting Unity project information
/// </summary>
public sealed class GetProjectInfoPayload
{
    public GetProjectInfoPayload() { }
}

/// <summary>
/// Payload for listing Unity scenes
/// </summary>
public sealed class ListScenesPayload
{
    public ListScenesPayload() { }
}

/// <summary>
/// Payload for ping/health check
/// </summary>
public sealed class PingPayload
{
    public PingPayload() { }
}

// Event payloads

/// <summary>
/// Event payload for log lines
/// </summary>
public sealed class LogLineEvent
{
    public string Line { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Information;

    public LogLineEvent() { }

    public LogLineEvent(string line, LogLevel level = LogLevel.Information)
    {
        Line = line;
        Level = level;
    }
}

/// <summary>
/// Event payload for command completion
/// </summary>
public sealed class RunFinishedEvent
{
    public int ExitCode { get; set; }
    public string? Error { get; set; }

    public RunFinishedEvent() { }

    public RunFinishedEvent(int exitCode, string? error = null)
    {
        ExitCode = exitCode;
        Error = error;
    }
}

/// <summary>
/// Event payload for Unity play state changes
/// </summary>
public sealed class PlayStateEvent
{
    public bool IsPlaying { get; set; }

    public PlayStateEvent() { }

    public PlayStateEvent(bool isPlaying)
    {
        IsPlaying = isPlaying;
    }
}

/// <summary>
/// Event payload for Unity compilation events
/// </summary>
public sealed class CompilationEvent
{
    public bool Started { get; set; }

    public CompilationEvent() { }

    public CompilationEvent(bool started)
    {
        Started = started;
    }
}

/// <summary>
/// Event payload for gate validation results
/// </summary>
public sealed class GateResultEvent
{
    public string GateName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Message { get; set; }

    public GateResultEvent() { }

    public GateResultEvent(string gateName, bool passed, string? message = null)
    {
        GateName = gateName;
        Passed = passed;
        Message = message;
    }
}

/// <summary>
/// Event payload for heartbeat/keepalive
/// </summary>
public sealed class HeartbeatEvent
{
    public DateTime Timestamp { get; set; }

    public HeartbeatEvent() { }

    public HeartbeatEvent(DateTime timestamp)
    {
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event payload for connection status
/// </summary>
public sealed class ConnectionEvent
{
    public bool Connected { get; set; }
    public string Message { get; set; } = string.Empty;

    public ConnectionEvent() { }

    public ConnectionEvent(bool connected, string message)
    {
        Connected = connected;
        Message = message;
    }
}

/// <summary>
/// Event payload for Unity project information
/// </summary>
public sealed class ProjectInfoEvent
{
    public string ProjectName { get; set; } = string.Empty;
    public string UnityVersion { get; set; } = string.Empty;
    public string ProjectPath { get; set; } = string.Empty;

    public ProjectInfoEvent() { }

    public ProjectInfoEvent(string projectName, string unityVersion, string projectPath)
    {
        ProjectName = projectName;
        UnityVersion = unityVersion;
        ProjectPath = projectPath;
    }
}

/// <summary>
/// Event payload for Unity scene list
/// </summary>
public sealed class SceneListEvent
{
    public string[] Scenes { get; set; } = Array.Empty<string>();

    public SceneListEvent() { }

    public SceneListEvent(string[] scenes)
    {
        Scenes = scenes;
    }
}
