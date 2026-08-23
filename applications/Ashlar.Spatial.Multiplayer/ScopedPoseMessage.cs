using Ashlar.Spatial.Contracts;

namespace Ashlar.Spatial.Multiplayer;

/// <summary>
/// Pose message relayed within a match scope.
/// </summary>
public sealed record ScopedPoseMessage(
    string ScopeId,
    string AtomId,
    PoseSample? Pose,
    bool Lost,
    string? SignalReason);
