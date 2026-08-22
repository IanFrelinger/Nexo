namespace Ashlar.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Thin native-interop seam for visionOS <c>ARKitSession</c>/<c>WorldTrackingProvider</c> pose lookup.
/// </summary>
/// <remarks>
/// <b>Session lifecycle ownership:</b> the host application owns session start/stop and immersive-space entry.
/// <see cref="IsImmersiveSpaceActive"/> must be true before poses are emitted — pre-immersive fail-closed.
/// </remarks>
public interface IVisionProNativeSession
{
    bool IsActive { get; }

    bool IsImmersiveSpaceActive { get; }

    bool IsInterrupted { get; }

    VisionProNativePoseFrame? TryGetAnchorPose(string atomId);

    IObservable<VisionProNativePoseFrame> ObserveAnchorPose(string atomId);
}
