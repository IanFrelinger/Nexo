using Nexo.Abstractions;

namespace Nexo.Mcp.Client;

/// <summary>
/// An <see cref="ITool"/> facade over one tool on one remote MCP server.
///
/// The id is namespaced <c>mcp:{server}:{tool}</c> so a remote server can never shadow a native
/// tool id (CapabilityRegistry registration is last-wins by id — an un-namespaced remote tool
/// called <c>repo.fs.write</c> would silently replace the real one). The schema is the pinned
/// definition captured at connection time; drift faults the proxy rather than following the
/// remote's new definition.
/// </summary>
public sealed class McpToolProxy : ITool
{
    private readonly McpClientConnectionManager _manager;

    internal McpToolProxy(
        McpClientConnectionManager manager,
        string serverName,
        string remoteToolName,
        ToolSchema schema)
    {
        _manager = manager;
        ServerName = serverName;
        RemoteToolName = remoteToolName;
        Schema = schema;
    }

    /// <summary>Logical name of the remote server this proxy belongs to.</summary>
    public string ServerName { get; }

    /// <summary>Tool name as advertised by the remote server.</summary>
    public string RemoteToolName { get; }

    /// <inheritdoc />
    public string Id => Schema.Id;

    /// <inheritdoc />
    public ToolSchema Schema { get; }

    /// <inheritdoc />
    public Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
        => _manager.InvokeProxiedToolAsync(this, toolCall.Arguments, s.Tick, ct);
}
