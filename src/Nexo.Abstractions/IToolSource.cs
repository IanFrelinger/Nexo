namespace Nexo.Abstractions;

/// <summary>
/// A provider of dynamically discovered tools.
///
/// Complements direct <see cref="ITool"/> registration for tools whose set is only known after
/// startup work has completed — e.g. tools proxied from remote MCP servers, which are discovered
/// by connecting and listing. Toolbox factories fold sources in at toolbox-build time, so a
/// source's tools become visible to agents without the source being constructible at DI
/// registration time.
///
/// Implementations must be safe to call repeatedly and should return a stable snapshot; an
/// empty list is the correct fail-closed answer while discovery is incomplete or has failed.
/// </summary>
public interface IToolSource
{
    /// <summary>Returns the currently available tools (possibly empty, never null).</summary>
    IReadOnlyList<ITool> GetTools();
}
