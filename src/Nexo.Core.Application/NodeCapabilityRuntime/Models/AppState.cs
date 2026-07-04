namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Application lifecycle state on the host platform.
/// </summary>
public enum AppState
{
    /// <summary>Application is in the foreground and fully active.</summary>
    Foreground,

    /// <summary>Application is backgrounded but may run limited work.</summary>
    Background,

    /// <summary>Background execution is restricted by the OS.</summary>
    BackgroundRestricted,

    /// <summary>Application process is suspended and cannot run work.</summary>
    Suspended
}
