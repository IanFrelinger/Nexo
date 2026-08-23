namespace Ashlar.Spatial.Platform.ARKit;

/// <summary>
/// Runtime probe for ARKit platform availability (no ARKit SDK reference in open core).
/// </summary>
public static class ArKitSpatialAvailability
{
    /// <summary>
    /// True when running on iOS 11+ where ARKit can be bound by a future native adapter.
    /// Always false on headless CI hosts (Linux, macOS desktop, Windows).
    /// </summary>
    public static bool IsSupported()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsIOSVersionAtLeast(11);
#else
        return false;
#endif
    }
}
