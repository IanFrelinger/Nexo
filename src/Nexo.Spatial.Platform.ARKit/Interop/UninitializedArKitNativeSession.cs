using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Fail-closed session placeholder used before the host wires a real ARKit session.
/// </summary>
public sealed class UninitializedArKitNativeSession : IArKitNativeSession
{
    public static UninitializedArKitNativeSession Instance { get; } = new();

    public bool IsActive => false;

    public bool IsInterrupted => false;

    public ArKitNativePoseFrame? TryGetAnchorPose(string atomId) => null;

    public IObservable<ArKitNativePoseFrame> ObserveAnchorPose(string atomId) =>
        Observable.Empty<ArKitNativePoseFrame>();
}
