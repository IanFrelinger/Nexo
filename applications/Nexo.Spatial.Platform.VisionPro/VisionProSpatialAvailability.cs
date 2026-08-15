using System.Runtime.InteropServices;

namespace Nexo.Spatial.Platform.VisionPro;

/// <summary>
/// Runtime probe for visionOS WorldTracking availability (no RealityKit/ARKit SDK reference in open core).
/// </summary>
/// <remarks>
/// Separate from <c>Nexo.Spatial.Platform.ARKit</c> for packaging: visionOS consumers depend on a visionOS-labelled
/// package without pulling the iOS ARKit platform assembly. Implementation mirrors ARKit-on-visionOS semantics but
/// adds an immersive-space lifecycle gate unique to visionOS apps.
/// </remarks>
public static class VisionProSpatialAvailability
{
    /// <summary>
    /// True on visionOS hosts where <c>ARKitSession</c>/<c>WorldTrackingProvider</c> can be bound by a future adapter.
    /// Always false on headless CI hosts.
    /// </summary>
    public static bool IsSupported()
    {
#if NET5_0_OR_GREATER
        return IsVisionOsRuntime();
#else
        return false;
#endif
    }

#if NET5_0_OR_GREATER
    private static bool IsVisionOsRuntime()
    {
#if NET9_0_OR_GREATER
        return OperatingSystem.IsVisionOS();
#else
        return OperatingSystem.IsIOSVersionAtLeast(1)
            && RuntimeInformation.OSDescription.Contains("visionOS", StringComparison.OrdinalIgnoreCase);
#endif
    }
#endif
}
