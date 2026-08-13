using System.Text;

namespace Nexo.Mcp.Server;

/// <summary>
/// Maps Nexo tool ids to MCP-facing tool names.
///
/// Nexo ids are dotted (<c>repo.fs.read</c>) but several MCP clients — including the Anthropic
/// API's tool-name validation — only accept <c>[A-Za-z0-9_-]</c>. We sanitize instead of passing
/// ids through so the same catalog works everywhere; the canonical id is kept alongside the
/// sanitized name in <see cref="NexoMcpToolDescriptor"/> for invocation and policy checks.
/// </summary>
public static class McpToolNaming
{
    /// <summary>Maximum MCP tool-name length accepted by the strictest known clients.</summary>
    public const int MaxNameLength = 64;

    /// <summary>
    /// Sanitizes a Nexo tool id into an MCP-safe name: every character outside
    /// <c>[A-Za-z0-9_-]</c> becomes '_'; the result is trimmed to <see cref="MaxNameLength"/>.
    /// The mapping is deterministic but not injective — <see cref="NexoMcpToolBridge"/> rejects
    /// catalogs where two canonical ids collapse to the same name rather than guessing.
    /// </summary>
    public static string Sanitize(string canonicalId)
    {
        if (string.IsNullOrWhiteSpace(canonicalId))
        {
            throw new ArgumentException("Tool id must be non-empty.", nameof(canonicalId));
        }

        var builder = new StringBuilder(canonicalId.Length);
        foreach (var c in canonicalId)
        {
            builder.Append(IsMcpSafe(c) ? c : '_');
        }

        var name = builder.ToString();
        return name.Length <= MaxNameLength ? name : name[..MaxNameLength];
    }

    private static bool IsMcpSafe(char c) =>
        c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-';
}
