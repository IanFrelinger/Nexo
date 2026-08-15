namespace Nexo.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Thin native-interop seam for XREAL NRSDK pose lookup.
/// </summary>
/// <remarks>
/// <para>
/// <b>SDK tier:</b> NRSDK on a tethered Android host phone (6DoF world-locked tracking).
/// Glasses relay pose through the phone; this provider reads the phone-side NRSDK session only.
/// </para>
/// <para>
/// <b>Session lifecycle ownership:</b> the host application owns NRSDK session start/stop.
/// Inject a session whose <see cref="IsActive"/> reflects the live NRSDK runtime.
/// </para>
/// </remarks>
public interface IXrealNativeSession
{
    bool IsActive { get; }

    bool IsHostDisconnected { get; }

    XrealNativePoseFrame? TryGetAnchorPose(string atomId);

    IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId);
}
