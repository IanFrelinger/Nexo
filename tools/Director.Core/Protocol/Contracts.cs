using System.Text.Json;

namespace Director.Core.Protocol;

/// <summary>
/// Payload for running Nexo CLI commands
/// </summary>
/// <param name="Command">Nexo command (analyze, validate, agent, etc.)</param>
/// <param name="Args">Command arguments</param>
/// <param name="WorkingDirectory">Working directory for the command</param>
public sealed record RunNexoPayload(string Command, string[] Args, string? WorkingDirectory = null);

/// <summary>
/// Payload for opening Unity scenes
/// </summary>
/// <param name="ScenePath">Path to the scene file</param>
public sealed record OpenScenePayload(string ScenePath);

/// <summary>
/// Payload for toggling Unity play mode
/// </summary>
/// <param name="ForceState">Force specific play state, null to toggle</param>
public sealed record TogglePlayPayload(bool? ForceState = null);

/// <summary>
/// Payload for applying UI modifications to Unity Editor
/// </summary>
/// <param name="TargetSlot">Target UI slot identifier</param>
/// <param name="UiSchema">UI schema definition as JSON</param>
public sealed record ApplyUIModPayload(string TargetSlot, JsonElement UiSchema);

/// <summary>
/// Payload for getting Unity project information
/// </summary>
public sealed record GetProjectInfoPayload();

/// <summary>
/// Payload for listing Unity scenes
/// </summary>
public sealed record ListScenesPayload();

/// <summary>
/// Payload for ping/health check
/// </summary>
public sealed record PingPayload();

// Event payloads

/// <summary>
/// Event payload for log lines
/// </summary>
/// <param name="Line">Log line content</param>
/// <param name="Level">Log level</param>
public sealed record LogLineEvent(string Line, LogLevel Level = LogLevel.Information);

/// <summary>
/// Event payload for command completion
/// </summary>
/// <param name="ExitCode">Process exit code</param>
/// <param name="Error">Error message if any</param>
public sealed record RunFinishedEvent(int ExitCode, string? Error = null);

/// <summary>
/// Event payload for Unity play state changes
/// </summary>
/// <param name="IsPlaying">Current play state</param>
public sealed record PlayStateEvent(bool IsPlaying);

/// <summary>
/// Event payload for Unity compilation events
/// </summary>
/// <param name="Started">True if compilation started, false if finished</param>
public sealed record CompilationEvent(bool Started);

/// <summary>
/// Event payload for gate validation results
/// </summary>
/// <param name="GateName">Name of the gate</param>
/// <param name="Passed">Whether the gate passed</param>
/// <param name="Message">Optional message</param>
public sealed record GateResultEvent(string GateName, bool Passed, string? Message = null);

/// <summary>
/// Event payload for heartbeat/keepalive
/// </summary>
/// <param name="Timestamp">Heartbeat timestamp</param>
public sealed record HeartbeatEvent(DateTime Timestamp);

/// <summary>
/// Event payload for connection status
/// </summary>
/// <param name="Connected">Connection status</param>
/// <param name="Message">Status message</param>
public sealed record ConnectionEvent(bool Connected, string Message);

/// <summary>
/// Event payload for Unity project information
/// </summary>
/// <param name="ProjectName">Unity project name</param>
/// <param name="UnityVersion">Unity version</param>
/// <param name="ProjectPath">Project root path</param>
public sealed record ProjectInfoEvent(string ProjectName, string UnityVersion, string ProjectPath);

/// <summary>
/// Event payload for Unity scene list
/// </summary>
/// <param name="Scenes">List of scene paths</param>
public sealed record SceneListEvent(string[] Scenes);
