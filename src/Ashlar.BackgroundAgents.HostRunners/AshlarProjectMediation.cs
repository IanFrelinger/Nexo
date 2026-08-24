using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Forge;

namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// M1 enforcement ordering: inside an ashlar project, self-extend writes are MEDIATED —
/// they land as held forge proposals instead of touching disk, and only a gate admission
/// applies them. Propose → hold → apply, with the hold before the write.
///
/// <para>This is not optional per host mood: the project's policy file existing is what
/// forces mediation, overriding whatever aggressiveness mode the host runs at. Sealed mode
/// then seals by construction — proposals are parked, the gate rejects, nothing is ever
/// applied, the disk is untouched.</para>
/// </summary>
public static class AshlarProjectMediation
{
    /// <summary>The project-local forge store — the same location <c>ashlar gates</c>
    /// resolves, so admission and application read one queue.</summary>
    public static ChangeProposalStore ProjectStore(string repoRoot) =>
        new(Path.Combine(repoRoot, ".ashlar", "forge"));

    /// <summary>True when the repo is an ashlar project (the policy file exists).</summary>
    public static bool IsAshlarProject(string repoRoot) =>
        File.Exists(Path.Combine(repoRoot, "ashlar.policy.yaml"));

    /// <summary>Ids currently in the store — snapshot before a cycle, diff after, and the
    /// difference is what the cycle proposed.</summary>
    public static IReadOnlySet<string> SnapshotIds(ChangeProposalStore store) =>
        store.List().Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
}

/// <summary>
/// A mode store pinned to one value. Used to force <see cref="ForgeMediatedWritesPolicy"/>
/// mediation inside ashlar projects regardless of the host's runtime mode.
/// </summary>
public sealed class FixedAggressivenessModeStore : IAggressivenessModeStore
{
    private readonly BackgroundAgentAggressivenessMode _mode;

    /// <summary>Creates a store that always reports <paramref name="mode"/>.</summary>
    public FixedAggressivenessModeStore(BackgroundAgentAggressivenessMode mode) => _mode = mode;

    /// <inheritdoc />
    public BackgroundAgentAggressivenessMode GetMode() => _mode;

    /// <inheritdoc />
    public void SetMode(BackgroundAgentAggressivenessMode mode) =>
        throw new InvalidOperationException(
            "Mediation is enforced by ashlar.policy.yaml; the mode is not switchable at runtime. "
            + "Change the project's policy, not the host's mood.");
}
