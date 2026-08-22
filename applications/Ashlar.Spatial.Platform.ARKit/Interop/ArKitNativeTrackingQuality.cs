namespace Ashlar.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Mirrors <c>ARCamera.TrackingState</c> without referencing the ARKit SDK in the open core.
/// </summary>
public enum ArKitNativeTrackingQuality
{
    Normal,
    Limited,
    NotAvailable
}
