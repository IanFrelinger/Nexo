using Nexo.Spatial.Contracts;
using Nexo.Spatial.Multiplayer;

namespace Nexo.Spatial.Runtime;

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

    /// <summary>Whether the host binding was attached to the scoped relay.</summary>
    public bool Succeeded { get; }

    /// <summary>Machine-readable rejection code when <see cref="Succeeded"/> is false.</summary>
    public string? RejectionCode { get; }

    /// <summary>Human-readable rejection detail when <see cref="Succeeded"/> is false.</summary>
    public string? Reason { get; }

    /// <summary>Creates a successful attach result.</summary>
    public static RelayAttachResult Success() => new(true, null, null);

    /// <summary>Creates a rejected attach result with code and reason.</summary>
    public static RelayAttachResult Rejected(string rejectionCode, string reason) =>
        new(false, rejectionCode, reason);
}
