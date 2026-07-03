using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Fail-closed session placeholder used before the host enters immersive space and starts WorldTracking.
/// </summary>
public sealed class UninitializedVisionProNativeSession : IVisionProNativeSession
{
    public static UninitializedVisionProNativeSession Instance { get; } = new();

    public bool IsActive => false;

    public bool IsImmersiveSpaceActive => false;

    public bool IsInterrupted => false;

    public VisionProNativePoseFrame? TryGetAnchorPose(string atomId) => null;

    public IObservable<VisionProNativePoseFrame> ObserveAnchorPose(string atomId) =>
        Observable.Empty<VisionProNativePoseFrame>();
}
