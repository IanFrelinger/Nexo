namespace Nexo.Spatial.Platform.ARKit.Interop;

/// <summary>
/// Thin native-interop seam for ARKit session pose lookup.
/// Real ARKit bindings live behind this interface; headless hosts use <see cref="UninitializedArKitNativeSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Session lifecycle ownership:</b> the host application owns ARKit session start/stop.
/// Inject an <see cref="IArKitNativeSession"/> whose <see cref="IsActive"/> reflects the live session.
/// This provider never starts or stops ARKit — it only reads poses from an already-active session.
/// </para>
/// </remarks>
public interface IArKitNativeSession
{
    /// <summary>True when the host has started an ARKit session and tracking may proceed.</summary>
    bool IsActive { get; }

    /// <summary>True when ARKit reported a session interruption (e.g. app backgrounded).</summary>
    bool IsInterrupted { get; }

    /// <summary>
    /// One-shot anchor pose lookup. Returns null when the anchor is not registered or tracking is unavailable.
    /// </summary>
    ArKitNativePoseFrame? TryGetAnchorPose(string atomId);

    /// <summary>
    /// Push stream of raw anchor pose frames for <paramref name="atomId"/>.
    /// Emits <see cref="ArKitNativeTrackingQuality.NotAvailable"/> on interruption; never silently completes without signal.
    /// </summary>
    IObservable<ArKitNativePoseFrame> ObserveAnchorPose(string atomId);
}
