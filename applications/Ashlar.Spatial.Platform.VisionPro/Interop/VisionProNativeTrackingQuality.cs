namespace Ashlar.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Mirrors visionOS <c>WorldTrackingProvider</c> quality without SDK references in the open core.
/// </summary>
public enum VisionProNativeTrackingQuality
{
    Normal,
    Limited,
    NotAvailable
}
