using System.Reactive.Linq;

namespace Ashlar.Spatial.Platform.XREAL.Interop;

/// <summary>
/// Fail-closed session placeholder used before the host wires NRSDK.
/// </summary>
public sealed class UninitializedXrealNativeSession : IXrealNativeSession
{
    /// <summary>Shared fail-closed session singleton.</summary>
    public static UninitializedXrealNativeSession Instance { get; } = new();

    /// <inheritdoc />
    public bool IsActive => false;

    /// <inheritdoc />
    public bool IsHostDisconnected => false;

    /// <inheritdoc />
    public XrealNativePoseFrame? TryGetAnchorPose(string atomId) => null;

    /// <inheritdoc />
    public IObservable<XrealNativePoseFrame> ObserveAnchorPose(string atomId) =>
        Observable.Empty<XrealNativePoseFrame>();
}
