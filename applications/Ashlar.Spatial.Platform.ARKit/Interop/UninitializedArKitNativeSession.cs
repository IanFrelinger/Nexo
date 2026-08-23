using System.Reactive.Linq;

namespace Ashlar.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Fail-closed session placeholder used before the host wires a real ARKit session.
/// </summary>
public sealed class UninitializedArKitNativeSession : IArKitNativeSession
{
    /// <summary>Shared fail-closed session singleton.</summary>
    public static UninitializedArKitNativeSession Instance { get; } = new();

    /// <inheritdoc />
    public bool IsActive => false;

    /// <inheritdoc />
    public bool IsInterrupted => false;

    /// <inheritdoc />
    public ArKitNativePoseFrame? TryGetAnchorPose(string atomId) => null;

    /// <inheritdoc />
    public IObservable<ArKitNativePoseFrame> ObserveAnchorPose(string atomId) =>
        Observable.Empty<ArKitNativePoseFrame>();
}
