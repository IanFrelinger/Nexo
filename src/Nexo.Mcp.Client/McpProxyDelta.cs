using Nexo.Abstractions;

namespace Nexo.Mcp.Client;

/// <summary>
/// Minimal <see cref="IActionDelta"/> for proxied MCP tool results. Remote tools have no Nexo
/// world-state to delta; this carries only the invocation log line so policy signing and delta
/// merging keep working. Deliberately internal — it is an adapter detail, not a contract.
/// </summary>
internal sealed class McpProxyDelta : IActionDelta
{
    private readonly List<string> _log = new();

    public McpProxyDelta(int tick, string logLine)
    {
        TickFrom = tick;
        TickTo = tick + 1;
        _log.Add(logLine);
    }

    /// <inheritdoc />
    public int TickFrom { get; }

    /// <inheritdoc />
    public int TickTo { get; }

    /// <inheritdoc />
    public IReadOnlyList<byte>? Signature { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<string> Log => _log;
}
