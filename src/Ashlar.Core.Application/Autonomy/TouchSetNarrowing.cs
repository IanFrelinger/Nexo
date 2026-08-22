using System.Diagnostics.CodeAnalysis;

namespace Ashlar.Core.Application.Autonomy;

/// <summary>
/// R4.4: a depth-n+1 session's capability envelope must be a SUBSET of the depth-n
/// session that produced its proposer context — capability monotonically narrows with
/// depth, it never widens. This is the session-level extension of the
/// <c>AgentPolicyNarrowingValidator</c> child⊆creator rule, expressed over touch-sets so
/// the loop harness can refuse a widening child before it ever opens.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class TouchSetNarrowing
{
    /// <summary>
    /// Every way <paramref name="child"/> widens beyond <paramref name="parent"/>.
    /// Empty = the child is within its parent's envelope. Paths and namespaces narrow by
    /// prefix (a child surface must sit inside some parent surface); capabilities narrow
    /// by set inclusion.
    /// </summary>
    public static IReadOnlyList<string> FindWidenings(TouchSet child, TouchSet parent)
    {
        if (child is null)
            throw new ArgumentNullException(nameof(child));
        if (parent is null)
            throw new ArgumentNullException(nameof(parent));

        var normalizedChild = child.Normalize();
        var normalizedParent = parent.Normalize();
        var widenings = new List<string>();

        foreach (var path in normalizedChild.PathPrefixes)
        {
            if (!normalizedParent.PathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                widenings.Add($"path {path} is outside every parent surface");
        }

        foreach (var ns in normalizedChild.Namespaces)
        {
            if (!normalizedParent.Namespaces.Any(p =>
                    ns.Equals(p, StringComparison.Ordinal) || ns.StartsWith(p + ".", StringComparison.Ordinal)))
            {
                widenings.Add($"namespace {ns} is outside every parent namespace");
            }
        }

        foreach (var capability in normalizedChild.Capabilities)
        {
            if (!normalizedParent.Capabilities.Contains(capability, StringComparer.Ordinal))
                widenings.Add($"capability {capability} is absent from the parent whitelist");
        }

        return widenings;
    }
}
