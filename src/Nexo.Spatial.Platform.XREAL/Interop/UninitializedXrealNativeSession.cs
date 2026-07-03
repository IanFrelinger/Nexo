using System.Reactive.Linq;

namespace Nexo.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Fail-closed session placeholder used before the host wires NRSDK.
/// </summary>
public sealed class UninitializedXrealNativeSession : IXrealNativeSession
{
    public static UninitializedXrealNativeSession Instance { get; } = new();

    public bool IsActive => false;

    public bool IsHostDisconnected => false;

    public XrealNativePoseFrame? TryGetAnchorPose(string atomId) => null;

    public IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId) =>
        Observable.Empty<XrealNativePoseFrame>();
}
