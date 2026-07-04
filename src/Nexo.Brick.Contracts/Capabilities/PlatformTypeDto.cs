namespace Nexo.Brick.Contracts.Capabilities;

/// <summary>Operating system platform reported by a Nexo node.</summary>
public enum PlatformTypeDto
{
    /// <summary>Microsoft Windows host.</summary>
    Windows,

    /// <summary>Apple macOS host.</summary>
    macOS,

    /// <summary>Linux host.</summary>
    Linux,

    /// <summary>Apple iOS device or simulator.</summary>
    iOS,

    /// <summary>Google Android device or emulator.</summary>
    Android,

    /// <summary>Platform could not be determined or is not classified.</summary>
    Unknown
}
