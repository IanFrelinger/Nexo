namespace Ashlar.Mcp.Client;

/// <summary>
/// Payload returned by proxied MCP tools when the remote call fails (remote <c>isError</c>
/// result, faulted pin, transport error). Follows the repo tool convention of reporting
/// business-level failures in the payload — visible to the calling model so it can correct
/// itself — rather than throwing.
/// </summary>
/// <param name="Error">Human/model-readable failure description.</param>
public sealed record McpToolFailure(string Error)
{
    /// <summary>Always true; present so JSON consumers can discriminate failure payloads.</summary>
    public bool IsError => true;
}
