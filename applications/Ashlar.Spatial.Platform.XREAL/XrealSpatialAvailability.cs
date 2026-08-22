namespace Ashlar.Spatial.Platform.XREAL;

/// <summary>
/// Runtime probe for XREAL NRSDK availability (no SDK reference in open core).
/// </summary>
public static class XrealSpatialAvailability
{
    /// <summary>
    /// True when running on Android (NRSDK host) where a future native adapter can bind tracking.
    /// Always false on headless CI hosts (Linux, macOS desktop, Windows).
    /// </summary>
    public static bool IsSupported()
    {
#if NET5_0_OR_GREATER
        return OperatingSystem.IsAndroid();
#else
        return false;
#endif
    }
}
