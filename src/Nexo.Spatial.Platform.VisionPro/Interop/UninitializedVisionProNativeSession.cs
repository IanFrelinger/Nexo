using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.VisionPro.Interop;

/// <summary>
/// Fail-closed session placeholder used before the host enters immersive space and starts WorldTracking.
/// </summary>
public sealed class UninitializedVisionProNativeSession : IVisionProNativeSession
{
    /// <summary>Shared fail-closed session singleton.</summary>
    public static UninitializedVisionProNativeSession Instance { get; } = new();

    /// <inheritdoc />
    public bool IsActive => false;

    /// <inheritdoc />
    public bool IsImmersiveSpaceActive => false;

    /// <inheritdoc />
    public bool IsInterrupted => false;

    /// <inheritdoc />
    public VisionProNativePoseFrame? TryGetAnchorPose(string atomId) => null;

    /// <inheritdoc />
    public IObservable<VisionProNativePoseFrame> ObserveAnchorPose(string atomId) =>
        Observable.Empty<VisionProNativePoseFrame>();
}
