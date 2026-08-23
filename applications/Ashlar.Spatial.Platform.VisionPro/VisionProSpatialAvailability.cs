using System.Runtime.InteropServices;

namespace Ashlar.Spatial.Platform.VisionPro;

/// <summary>
/// Runtime probe for visionOS WorldTracking availability (no RealityKit/ARKit SDK reference in open core).
/// </summary>
/// <remarks>
/// Separate from <c>Ashlar.Spatial.Platform.ARKit</c> for packaging: visionOS consumers depend on a visionOS-labelled
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
        // NOT gated on NET9_0_OR_GREATER. OperatingSystem.IsVisionOS() exists only when the
        // TFM carries the visionOS platform (net10.0-ios and friends); on the plain net10.0
        // leg this project multi-targets, the method is absent and the call was a hard
        // CS0117 — which went unnoticed because Ashlar.sln never restored, so this leg was
        // never compiled. The portable heuristic below builds on every TFM here (netstandard2.0
        // excluded by the outer #if) and matches the behaviour the net8.0 leg already shipped.
        return OperatingSystem.IsIOSVersionAtLeast(1)
            && RuntimeInformation.OSDescription.Contains("visionOS", StringComparison.OrdinalIgnoreCase);
    }
#endif
}
