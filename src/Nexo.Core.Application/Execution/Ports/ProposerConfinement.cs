namespace Nexo.Core.Application.Execution.Ports;

/// <summary>
/// The single declaration of what a proposer session may touch (extension spec Part B):
/// one object derives BOTH the in-process tool write-allowlist AND the sandbox bind
/// mounts, so the paths an agent's tools may write and the paths its container can
/// physically reach can never drift apart — the same single-source-used-twice discipline
/// the constraint manifest applies to prompts and gates.
///
/// The derived physical shape is: the whole repository mounted read-only at
/// <see cref="ContainerWorkspaceRoot"/> (context is readable), with each
/// <see cref="WritablePrefixes"/> entry re-mounted read-write on top (writes land only in
/// declared surfaces).
/// </summary>
public sealed record ProposerConfinement
{
    /// <summary>
    /// Repo-relative directory prefixes the proposer may write (e.g. <c>src/</c>,
    /// <c>.nexo/</c>). Everything else is read-only context. Entries are normalized to
    /// forward slashes with a trailing slash; rooted paths and traversal are rejected.
    /// </summary>
    public required IReadOnlyList<string> WritablePrefixes { get; init; }

    /// <summary>Container-side path the repository maps under. Default <c>/workspace</c>.</summary>
    public string ContainerWorkspaceRoot { get; init; } = "/workspace";

    /// <summary>
    /// First derivation: the exact write-allowlist prefixes for the in-process tool policy.
    /// Normalized, validated, and free of nesting — see <see cref="Validate"/>.
    /// </summary>
    public IReadOnlyList<string> ToPathAllowlistPrefixes() => Validate();

    /// <summary>
    /// Second derivation: the bind mounts realizing the same declaration physically.
    /// The repository root is mounted read-only; each writable prefix is mounted
    /// read-write on top of it.
    /// </summary>
    public IReadOnlyList<Mount> ToMounts(string hostRepoRoot)
    {
        if (string.IsNullOrWhiteSpace(hostRepoRoot))
            throw new ArgumentException("Host repo root is required.", nameof(hostRepoRoot));

        var prefixes = Validate();
        var hostRoot = hostRepoRoot.Replace('\\', '/').TrimEnd('/');
        var workspace = ContainerWorkspaceRoot.TrimEnd('/');

        var mounts = new List<Mount> { new(hostRoot, workspace, ReadOnly: true) };
        foreach (var prefix in prefixes)
        {
            var sub = prefix.TrimEnd('/');
            mounts.Add(new Mount($"{hostRoot}/{sub}", $"{workspace}/{sub}", ReadOnly: false));
        }

        return mounts;
    }

    /// <summary>
    /// Normalizes and validates the declaration. Refused loudly rather than repaired:
    /// empty declarations (a confinement that allows nothing is a wiring bug, not a
    /// stricter sandbox), rooted paths, traversal, and NESTED writable prefixes — with
    /// bind mounts, nested surfaces make effective permissions mount-order-dependent,
    /// which is exactly the silent-drift class this type exists to kill.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        if (WritablePrefixes is null || WritablePrefixes.Count == 0)
        {
            throw new InvalidOperationException(
                "ProposerConfinement declares no writable prefixes. A proposer that can write "
                + "nothing cannot propose; declare the writable surfaces explicitly.");
        }

        var normalized = new List<string>();
        foreach (var raw in WritablePrefixes)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("ProposerConfinement contains a blank writable prefix.");

            var prefix = raw.Replace('\\', '/').Trim().TrimStart('/');
            if (prefix.Length == 0 || prefix.IndexOf("..", StringComparison.Ordinal) >= 0 || prefix.IndexOf(':') >= 0)
            {
                throw new InvalidOperationException(
                    $"ProposerConfinement writable prefix '{raw}' is not a clean repo-relative path.");
            }

            if (!prefix.EndsWith("/", StringComparison.Ordinal))
                prefix += "/";
            normalized.Add(prefix);
        }

        for (var i = 0; i < normalized.Count; i++)
        {
            for (var j = 0; j < normalized.Count; j++)
            {
                if (i != j && normalized[i].StartsWith(normalized[j], StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"ProposerConfinement writable prefixes overlap: '{normalized[i]}' is inside "
                        + $"'{normalized[j]}'. Overlapping mounts make effective permissions "
                        + "mount-order-dependent; declare disjoint surfaces.");
                }
            }
        }

        return normalized;
    }
}
