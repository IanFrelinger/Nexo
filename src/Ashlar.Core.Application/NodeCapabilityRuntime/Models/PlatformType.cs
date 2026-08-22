namespace Ashlar.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Host platform of the current runtime.
/// </summary>
public enum PlatformType
{
    /// <summary>Microsoft Windows desktop or server.</summary>
    Windows,

    /// <summary>Apple macOS desktop.</summary>
    macOS,

    /// <summary>Linux desktop or server.</summary>
    Linux,

    /// <summary>Apple iOS mobile or tablet.</summary>
    iOS,

    /// <summary>Google Android mobile or tablet.</summary>
    Android,

    /// <summary>Platform could not be determined.</summary>
    Unknown
}
