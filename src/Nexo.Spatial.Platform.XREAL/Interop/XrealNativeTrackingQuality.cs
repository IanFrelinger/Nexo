namespace Nexo.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Mirrors NRSDK tracking quality without referencing the XREAL SDK in the open core.
/// </summary>
public enum XrealNativeTrackingQuality
{
    Tracking,
    Limited,
    Lost
}
