using Nexo.Core.Application.Execution.Ports;

namespace Nexo.Core.Application.Autonomy;

/// <summary>
/// The declared blast radius of one objective (autonomous self-extension spec R1.3): the
/// files, namespaces, and capabilities the extension is permitted to affect. One
/// declaration, three enforcement surfaces — it classifies the tier (§3), derives the
/// session's sandbox mounts (via <see cref="ToProposerConfinement"/>), and (U2) is
/// enforced by the analyzer gate against the candidate's actual reference graph.
///
/// An objective whose touch-set is undeclared is not "unbounded" — it is classified at
/// the most restrictive applicable tier (R1.4) and, having no writable surfaces, its
/// session physically cannot write anywhere.
/// </summary>
public sealed record TouchSet
{
    /// <summary>Repo-relative directory prefixes the extension may write (ProposerConfinement grammar).</summary>
    public IReadOnlyList<string> PathPrefixes { get; init; } = Array.Empty<string>();

    /// <summary>Namespace prefixes the extension's code may declare types in or reference beyond the baseline.</summary>
    public IReadOnlyList<string> Namespaces { get; init; } = Array.Empty<string>();

    /// <summary>Tool/capability ids the proposal session may use (subset semantics per R4.4).</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();

    /// <summary>An empty touch-set is "undeclared" — nothing was statically determined (R1.4).</summary>
    public bool IsDeclared =>
        PathPrefixes.Count > 0 || Namespaces.Count > 0 || Capabilities.Count > 0;

    /// <summary>
    /// Derives the session confinement from this declaration (same object that classified
    /// the tier — single source, second surface). Requires at least one path prefix:
    /// a session for an extension that may write nothing is a wiring bug.
    /// </summary>
    public ProposerConfinement ToProposerConfinement() =>
        new() { WritablePrefixes = PathPrefixes };

    /// <summary>
    /// Normalizes and validates the declaration. Delegates path grammar to
    /// <see cref="ProposerConfinement.Validate"/> when paths are present (identical rules,
    /// by construction); namespace entries must be clean dotted identifiers.
    /// </summary>
    public TouchSet Normalize()
    {
        var paths = PathPrefixes.Count == 0
            ? Array.Empty<string>()
            : (IReadOnlyList<string>)new ProposerConfinement { WritablePrefixes = PathPrefixes }.Validate();

        var namespaces = new List<string>();
        foreach (var raw in Namespaces)
        {
            var ns = raw?.Trim() ?? "";
            if (ns.Length == 0 || !ns.Split('.').All(part =>
                    part.Length > 0 && char.IsLetter(part[0]) && part.All(c => char.IsLetterOrDigit(c) || c == '_')))
            {
                throw new InvalidOperationException(
                    $"TouchSet namespace '{raw}' is not a clean dotted identifier.");
            }

            namespaces.Add(ns);
        }

        var capabilities = Capabilities
            .Select(c => c?.Trim() ?? "")
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new TouchSet
        {
            PathPrefixes = paths,
            Namespaces = namespaces,
            Capabilities = capabilities,
        };
    }
}
