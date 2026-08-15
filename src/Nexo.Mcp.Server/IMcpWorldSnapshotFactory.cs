using Microsoft.Extensions.Options;
using Nexo.Abstractions;

namespace Nexo.Mcp.Server;

/// <summary>
/// Produces the <see cref="WorldSnapshot"/> handed to tools for MCP-originated invocations.
///
/// MCP callers have no simulation context, so unlike <c>AgentHost</c>-driven calls there is no
/// ambient snapshot to reuse. The default implementation seeds a repo-rooted snapshot from
/// host options; it must never be derived from caller-supplied request data.
/// </summary>
public interface IMcpWorldSnapshotFactory
{
    /// <summary>Creates the snapshot for one invocation of <paramref name="canonicalToolId"/>.</summary>
    WorldSnapshot Create(string canonicalToolId);
}

/// <summary>
/// Default snapshot factory: <see cref="WorldSnapshot.ForRepo"/> over
/// <see cref="NexoMcpServerOptions.RepoRoot"/> (falling back to the process working directory),
/// tick 0. Registered with TryAdd so hosts with a richer world model can substitute their own.
/// </summary>
public sealed class OptionsMcpWorldSnapshotFactory : IMcpWorldSnapshotFactory
{
    private readonly IOptions<NexoMcpServerOptions> _options;

    /// <summary>Creates the factory over the bound server options.</summary>
    public OptionsMcpWorldSnapshotFactory(IOptions<NexoMcpServerOptions> options) => _options = options;

    /// <inheritdoc />
    public WorldSnapshot Create(string canonicalToolId)
    {
        var opts = _options.Value;
        var repoRoot = string.IsNullOrWhiteSpace(opts.RepoRoot)
            ? Directory.GetCurrentDirectory()
            : opts.RepoRoot!;
        return WorldSnapshot.ForRepo(repoRoot, opts.OutputRoot);
    }
}
