using System.Reactive.Linq;
using System.Reactive.Subjects;
using Ashlar.Spatial.Contracts;

namespace Ashlar.Spatial.Multiplayer;

/// <summary>
/// In-memory scoped pose transport for headless rejection tests.
/// </summary>
public sealed class InMemoryScopedPoseTransport : IScopedPoseTransport, IDisposable
{
    private readonly Dictionary<string, HashSet<string>> _subscriptions = new(StringComparer.Ordinal);
    private readonly Dictionary<(string ScopeId, string ParticipantId), Subject<ScopedPoseMessage>> _subjects = new();
    private readonly object _gate = new();
    private bool _disposed;

    /// <summary>Publishes a scoped pose update to scope subscribers.</summary>
    public PoseRelayResult TryPublish(string scopeId, string publisherParticipantId, string atomId, PoseSample pose)
    {
        lock (_gate)
        {
            ObjectDisposedGuard();
            if (!_subscriptions.TryGetValue(scopeId, out var members))
                return PoseRelayResult.Rejected("scope-not-subscribed", "No subscribers for scope.");

            foreach (var member in members)
            {
                var key = (scopeId, member);
                if (_subjects.TryGetValue(key, out var subject) && !subject.IsDisposed)
                {
                    subject.OnNext(new ScopedPoseMessage(scopeId, atomId, pose, Lost: false, SignalReason: null));
                }
            }

            return PoseRelayResult.AcceptedResult();
        }
    }

    /// <summary>Subscribes a participant to scoped pose messages for a scope.</summary>
    public IObservable<ScopedPoseMessage> Subscribe(string scopeId, string subscriberParticipantId)
    {
        lock (_gate)
        {
            ObjectDisposedGuard();
            if (!_subscriptions.TryGetValue(scopeId, out var members))
            {
                members = new HashSet<string>(StringComparer.Ordinal);
                _subscriptions[scopeId] = members;
            }

            members.Add(subscriberParticipantId);

            var key = (scopeId, subscriberParticipantId);
            if (!_subjects.TryGetValue(key, out var subject) || subject.IsDisposed)
            {
                subject = new Subject<ScopedPoseMessage>();
                _subjects[key] = subject;
            }

            return subject.AsObservable();
        }
    }

    /// <summary>Broadcasts a lost-tracking signal to scope subscribers.</summary>
    public void PublishSignal(string scopeId, string atomId, string reason)
    {
        lock (_gate)
        {
            ObjectDisposedGuard();
            if (!_subscriptions.TryGetValue(scopeId, out var members))
                return;

            foreach (var member in members)
            {
                var key = (scopeId, member);
                if (_subjects.TryGetValue(key, out var subject) && !subject.IsDisposed)
                {
                    subject.OnNext(new ScopedPoseMessage(scopeId, atomId, Pose: null, Lost: true, SignalReason: reason));
                }
            }
        }
    }

    /// <summary>Disposes all active subscription subjects.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var subject in _subjects.Values)
            {
                subject.OnCompleted();
                subject.Dispose();
            }
        }
    }

    private void ObjectDisposedGuard()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryScopedPoseTransport));
    }
}
