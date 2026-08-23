namespace Ashlar.Core.Application.Adaptation.Ports;

/// <summary>
/// Registry of immutable core component identifiers.
/// Self-adaptation must never modify these components.
/// </summary>
public interface IImmutableCoreRegistry
{
    /// <summary>
    /// All component identifiers that are part of the immutable core.
    /// </summary>
    IReadOnlyList<string> CoreComponentIds { get; }

    /// <summary>
    /// Returns true if the given path or component identifier targets the immutable core.
    /// </summary>
    bool IsInImmutableCore(string pathOrComponentId);

    /// <summary>
    /// Returns true if the given namespace is part of the immutable core.
    /// </summary>
    bool IsCoreNamespace(string namespaceOrPath);
}
