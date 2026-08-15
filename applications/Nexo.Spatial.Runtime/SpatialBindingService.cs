using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Runtime.Ports;

namespace Nexo.Spatial.Runtime;

/// <summary>
/// Binds continuous pose tracking to certified physical atoms only.
/// This is the sole sprint seam allowed to depend on both identity and pose contracts.
/// </summary>
public sealed class SpatialBindingService : IDisposable
{
    private readonly IPhysicalAtomResolver _resolver;
    private readonly ISpatialAnchorProvider _provider;
    private readonly PoseStreamConsumer _consumer;
    private readonly Dictionary<string, BindingSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private bool _disposed;

    public SpatialBindingService(
        IPhysicalAtomResolver resolver,
        ISpatialAnchorProvider provider,
        PoseStreamConsumer? consumer = null)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _consumer = consumer ?? new PoseStreamConsumer();
    }

    /// <summary>Attempts a one-shot bind of pose tracking to a certified physical atom.</summary>
    public SpatialBindingResult TryBind(string markerPayload)
    {
        var resolve = _resolver.Resolve(markerPayload);
        if (!resolve.Found || resolve.Record is null)
            return SpatialBindingResult.Rejected("atom-not-certified");

        var pose = _provider.GetCurrentPose(resolve.Record.AtomId).GetAwaiter().GetResult();
        var processed = _consumer.Process(pose, previous: null, DateTimeOffset.UtcNow);
        if (!processed.Accepted || processed.EffectiveTrackingState == TrackingState.Lost)
        {
            return SpatialBindingResult.Rejected(
                processed.RejectionReason ?? "pose-unavailable");
        }

        return SpatialBindingResult.ActiveBinding(processed, resolve.Record);
    }

    /// <summary>Observes continuous bound pose updates for a certified atom.</summary>
    public IObservable<SpatialBindingUpdate> ObserveBoundPose(string atomId, string markerPayload)
    {
        lock (_gate)
        {
            ObjectDisposedGuard();

            var initialResolve = _resolver.Resolve(markerPayload);
            if (!initialResolve.Found || initialResolve.Record is null)
            {
                return Observable.Return(SpatialBindingUpdate.RejectedUpdate("atom-not-certified"));
            }

            if (!string.Equals(initialResolve.Record.AtomId, atomId, StringComparison.Ordinal))
                return Observable.Return(SpatialBindingUpdate.RejectedUpdate("atom-id-mismatch"));

            if (!_sessions.TryGetValue(atomId, out var session))
            {
                session = new BindingSession(atomId, markerPayload, _resolver, _provider, _consumer);
                _sessions[atomId] = session;
            }

            return session.Updates;
        }
    }

    /// <summary>Disposes all active binding sessions.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var session in _sessions.Values)
                session.Dispose();
            _sessions.Clear();
        }
    }

    private void ObjectDisposedGuard()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SpatialBindingService));
    }

    private sealed class BindingSession : IDisposable
    {
        private readonly string _atomId;
        private readonly string _markerPayload;
        private readonly IPhysicalAtomResolver _resolver;
        private readonly PoseStreamConsumer _consumer;
        private readonly Subject<SpatialBindingUpdate> _updates = new();
        private readonly IDisposable _subscription;
        private PoseSample? _previous;
        private ResolvedAtomIdentity? _activeIdentity;

        public BindingSession(
            string atomId,
            string markerPayload,
            IPhysicalAtomResolver resolver,
            ISpatialAnchorProvider provider,
            PoseStreamConsumer consumer)
        {
            _atomId = atomId;
            _markerPayload = markerPayload;
            _resolver = resolver;
            _consumer = consumer;

            var initial = _resolver.Resolve(_markerPayload);
            if (initial.Found)
                _activeIdentity = initial.Record;

            _subscription = provider.ObservePose(atomId).Subscribe(OnProviderPose);
        }

        public IObservable<SpatialBindingUpdate> Updates => _updates.AsObservable();

        private void OnProviderPose(PoseSample pose)
        {
            var now = DateTimeOffset.UtcNow;
            var resolve = _resolver.Resolve(_markerPayload);
            if (!resolve.Found || resolve.Record is null)
            {
                _activeIdentity = null;
                _updates.OnNext(SpatialBindingUpdate.RejectedUpdate("atom-not-certified"));
                return;
            }

            if (_activeIdentity is not null
                && !string.Equals(_activeIdentity.AssetHash, resolve.Record.AssetHash, StringComparison.Ordinal))
            {
                _activeIdentity = null;
                _updates.OnNext(SpatialBindingUpdate.RejectedUpdate("asset-hash-changed"));
                return;
            }

            _activeIdentity = resolve.Record;

            if (pose.TrackingState == TrackingState.Lost)
            {
                _previous = null;
                _updates.OnNext(SpatialBindingUpdate.LostUpdate("provider-lost"));
                return;
            }

            var processed = _consumer.Process(pose, _previous, now);
            _previous = pose;

            if (!processed.Accepted || processed.EffectiveTrackingState == TrackingState.Lost)
            {
                _updates.OnNext(SpatialBindingUpdate.RejectedUpdate(processed.RejectionReason ?? "pose-rejected"));
                return;
            }

            _updates.OnNext(SpatialBindingUpdate.ActiveUpdate(processed, resolve.Record));
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _updates.OnCompleted();
            _updates.Dispose();
        }
    }
}
