using System.Reactive.Linq;

namespace Nexo.Spatial.Contracts;

/// <summary>
/// Platform-agnostic spatial anchor contract for continuous pose tracking.
/// </summary>
/// <remarks>
/// <para>
/// Continuous pose delivery uses <see cref="ObservePose"/> with <see cref="IObservable{T}"/>
/// because pose updates are frame-rate-bound push streams. Pull-style <see cref="GetCurrentPose"/>
/// remains for one-shot queries.
/// </para>
/// <para>
/// Smoothing, velocity bounds, and discontinuity filtering intentionally live downstream
/// (see <see cref="PoseStreamConsumer"/>), not in provider implementations. Providers emit raw poses.
/// </para>
/// </remarks>
public interface ISpatialAnchorProvider
{
    Task<PoseSample?> GetCurrentPose(string atomId);

    IObservable<PoseSample> ObservePose(string atomId);
}
