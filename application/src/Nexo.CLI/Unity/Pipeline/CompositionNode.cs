namespace Nexo.CLI.Unity.Pipeline;

using System.Text.Json;

/// <summary>Single node in a Unity asset composition graph.</summary>
public sealed record CompositionNode
{
    /// <summary>Stable node identifier referenced by dependency edges.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Generation prompt describing assets or code to produce for this node.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Identifiers of upstream nodes that must complete before this node runs.</summary>
    public IReadOnlyList<string> Depends { get; init; } = Array.Empty<string>();
}
