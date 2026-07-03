using Nexo.Spatial.Contracts;
using Nexo.Spatial.Multiplayer;

namespace Nexo.Spatial.Runtime;

/// <summary>
/// Host-side bridge connecting <see cref="SpatialBindingService"/> output to scoped pose relay.
/// Lives in Runtime because it is the identity/pose seam composition point.
/// </summary>
public sealed class SpatialHostRelayBridge : IDisposable
{
    private readonly ScopedPoseRelay _relay;
    private readonly Dictionary<string, IDisposable> _bindingSubscriptions = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private bool _disposed;

    public SpatialHostRelayBridge(ScopedPoseRelay relay)
    {
        _relay = relay ?? throw new ArgumentNullException(nameof(relay));
    }

    public RelayAttachResult AttachHostBinding(
        string scopeId,
        string hostParticipantId,
        string atomId,
        string markerPayload,
        SpatialBindingService bindingService)
    {
        var scope = _relay.ScopeStore.GetScope(scopeId);
        if (scope is null)
            return RelayAttachResult.Rejected("scope-not-found", $"Scope '{scopeId}' does not exist.");

        if (!string.Equals(scope.HostParticipantId, hostParticipantId, StringComparison.Ordinal))
        {
            return RelayAttachResult.Rejected(
                "not-scope-host",
                $"Participant '{hostParticipantId}' is not the host for scope '{scopeId}'.");
        }

        if (!scope.ScopedAtomIds.Any(id => string.Equals(id, atomId, StringComparison.Ordinal)))
        {
            return RelayAttachResult.Rejected(
                "atom-not-in-scope",
                $"Atom '{atomId}' is not in scope '{scopeId}'.");
        }

        var key = $"{scopeId}:{atomId}";
        lock (_gate)
        {
            ObjectDisposedGuard();
            if (_bindingSubscriptions.ContainsKey(key))
                return RelayAttachResult.Rejected("binding-already-attached", $"Binding already attached for '{key}'.");

            var subscription = bindingService
                .ObserveBoundPose(atomId, markerPayload)
                .Subscribe(update =>
                {
                    if (update.Lost)
                    {
                        _relay.Host.PublishHostLost(scopeId, atomId, update.RejectionCode ?? "provider-lost");
                        return;
                    }

                    if (!update.Active || update.ProcessedPose?.Pose is null)
                        return;

                    _relay.Host.TryPublishPose(scopeId, hostParticipantId, atomId, update.ProcessedPose.Pose);
                });

            _bindingSubscriptions[key] = subscription;
        }

        return RelayAttachResult.Success();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var subscription in _bindingSubscriptions.Values)
                subscription.Dispose();
            _bindingSubscriptions.Clear();
        }
    }

    private void ObjectDisposedGuard()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SpatialHostRelayBridge));
    }
}

/// <summary>
/// Result of attaching a host binding to a scoped relay.
/// </summary>
public sealed class RelayAttachResult
{
    public RelayAttachResult(bool succeeded, string? rejectionCode, string? reason)
    {
        Succeeded = succeeded;
        RejectionCode = rejectionCode?.Trim();
        Reason = reason?.Trim();
    }

    public bool Succeeded { get; }

    public string? RejectionCode { get; }

    public string? Reason { get; }

    public static RelayAttachResult Success() => new(true, null, null);

    public static RelayAttachResult Rejected(string rejectionCode, string reason) =>
        new(false, rejectionCode, reason);
}
