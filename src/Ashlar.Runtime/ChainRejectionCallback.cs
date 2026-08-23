using Ashlar.Abstractions;

namespace Ashlar.Runtime;

/// <summary>
/// Callback invoked when a tool call chain is rejected before any execution.
/// (chain, rejectedIndex, reason) — enables audit logging with full chain trace.
/// </summary>
public delegate void ChainRejectionCallback(IReadOnlyList<ToolCall> chain, int rejectedIndex, string reason);
