namespace Ashlar.Mcp.Client;

/// <summary>
/// Options for consuming external MCP servers as Ashlar tools.
///
/// Fail-closed by design: <see cref="Enabled"/> defaults to <see langword="false"/>, the server
/// list defaults to empty, and per-server <see cref="McpServerEndpointOptions.AllowedTools"/>
/// narrows what a remote server may contribute. v1 supports streamable-HTTP servers only —
/// stdio child processes are deliberately excluded until a command-allowlist design exists
/// (spawning operator-configurable processes is a much sharper edge than dialing a URL).
/// </summary>
public sealed class AshlarMcpClientOptions
{
    /// <summary>Default configuration section (<c>Ashlar:Mcp:Client</c>).</summary>
    public const string SectionPath = "Ashlar:Mcp:Client";

    /// <summary>Master switch; defaults to <see langword="false"/> (no connections are dialed).</summary>
    public bool Enabled { get; set; }

    /// <summary>External MCP servers to connect to.</summary>
    public IList<McpServerEndpointOptions> Servers { get; } = new List<McpServerEndpointOptions>();

    /// <summary>Per-server connect + initial tool-list budget.</summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Interval for re-listing remote tools to detect definition drift. Pinned definitions are
    /// the contract: a tool whose schema or description changes server-side is marked faulted
    /// (fail-closed) until the host restarts and re-pins. <see cref="TimeSpan.Zero"/> disables
    /// the drift check.
    /// </summary>
    public TimeSpan ToolListRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>One external MCP server connection.</summary>
public sealed class McpServerEndpointOptions
{
    /// <summary>
    /// Logical server name; becomes the tool id prefix (<c>mcp:{Name}:{tool}</c>), so it is
    /// restricted to <c>[A-Za-z0-9_-]</c> and must be unique.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute http(s) endpoint of the server's streamable-HTTP transport.</summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional header name carrying the server's API key (e.g. <c>Authorization</c> or
    /// <c>X-Api-Key</c>). Must be paired with <see cref="ApiKeyEnvVar"/>.
    /// </summary>
    public string? ApiKeyHeader { get; set; }

    /// <summary>
    /// Environment variable holding the API key value. The secret itself never lives in bound
    /// configuration files — only the variable name does.
    /// </summary>
    public string? ApiKeyEnvVar { get; set; }

    /// <summary>
    /// Optional narrowing allowlist of remote tool names. Empty means "all tools the server
    /// advertises" — the server was already explicitly configured, which is the trust decision;
    /// use this to trim over-broad servers.
    /// </summary>
    public IList<string> AllowedTools { get; } = new List<string>();
}
