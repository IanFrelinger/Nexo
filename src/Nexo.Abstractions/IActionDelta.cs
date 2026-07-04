using System.Text.Json;

namespace Nexo.Abstractions;

/// <summary>
/// Represents a delta (change) to the world state resulting from tool execution.
///
/// Contains:
/// - Tick range (from/to) indicating when the change occurred
/// - Cryptographic signature for integrity verification
/// - Log messages describing what happened
///
/// Action deltas are merged together to form the complete world state change.
/// Used by PolicyEngine for signing and verification.
/// </summary>
public interface IActionDelta
{
    /// <summary>
    /// Starting tick of this delta.
    /// </summary>
    int TickFrom { get; }

    /// <summary>
    /// Ending tick of this delta.
    /// </summary>
    int TickTo { get; }

    /// <summary>
    /// Cryptographic signature (SHA256 hash) for integrity verification.
    /// </summary>
    IReadOnlyList<byte>? Signature { get; set; }

    /// <summary>
    /// Log messages describing the actions taken.
    /// </summary>
    IReadOnlyList<string> Log { get; }
}
