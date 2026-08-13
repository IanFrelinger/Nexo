using System.Collections.Concurrent;
using Nexo.Core.Application.Generation.Ports;
using Nexo.Core.Application.Orchestration;
using Nexo.Core.Domain.Bricks.Ports;

namespace Nexo.Core.Application.Generation;

/// <summary>
/// Optional DI contribution that supplies an <see cref="AgentProfile"/> at registry construction.
/// </summary>
public interface IAgentProfileSource
{
    /// <summary>Builds the profile.</summary>
    AgentProfile Create(IServiceProvider services);
}

/// <summary>In-memory <see cref="IAgentProfileRegistry"/>.</summary>
public sealed class AgentProfileRegistry : IAgentProfileRegistry
{
    private readonly ConcurrentDictionary<string, AgentProfile> _profiles =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates an empty registry.</summary>
    public AgentProfileRegistry()
    {
    }

    /// <summary>Creates a registry and eagerly loads DI profile sources.</summary>
    public AgentProfileRegistry(IEnumerable<IAgentProfileSource> sources, IServiceProvider services)
    {
        foreach (var source in sources ?? Array.Empty<IAgentProfileSource>())
            Register(source.Create(services));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> TargetIds => _profiles.Keys.ToArray();

    /// <inheritdoc />
    public AgentProfile? Resolve(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return null;
        return _profiles.TryGetValue(targetId.Trim(), out var profile) ? profile : null;
    }

    /// <inheritdoc />
    public void Register(AgentProfile profile)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (string.IsNullOrWhiteSpace(profile.TargetId))
            throw new ArgumentException("AgentProfile.TargetId is required.", nameof(profile));

        var declared = profile.Validators ?? Array.Empty<object>();

        // Fail loudly on validators the engine would silently skip: the engine
        // filters with OfType<IPostValidator<GeneratedArtifact>>, so a mistyped
        // validator would otherwise mean "no validation at all" with no error —
        // a silent verification downgrade. Make it a registration error instead.
        var unusable = declared
            .Where(v => v is not IPostValidator<GeneratedArtifact>)
            .Select(v => v?.GetType().FullName ?? "(null)")
            .ToArray();
        if (unusable.Length > 0)
        {
            throw new ArgumentException(
                $"AgentProfile '{profile.TargetId}' declares validator(s) the engine cannot use: "
                + string.Join(", ", unusable)
                + ". Validators must implement IPostValidator<GeneratedArtifact>.",
                nameof(profile));
        }

        // Same family of bug, arrived at from the other side: a profile with NO
        // usable validator passes the check above vacuously, then reports
        // verified with no gate ever run. The deterministic branch mints
        // verified:true unconditionally, and the final-provenance pass re-runs
        // validators only when there is at least one — so zero validators means
        // "verified" is a claim about nothing. A verdict must be backed by at
        // least one real gate, so refuse the profile at registration rather than
        // letting it mint hollow verdicts for the rest of the process lifetime.
        // A declared constraint manifest counts as a gate: the engine prepends
        // BrickConstraintManifestValidator unconditionally in ResolveValidators, so a
        // manifest-bearing profile can never run ungated even with zero declared validators.
        if (profile.ConstraintManifest is null &&
            !declared.OfType<IPostValidator<GeneratedArtifact>>().Any())
        {
            throw new ArgumentException(
                $"AgentProfile '{profile.TargetId}' declares no usable validators and no constraint "
                + "manifest. A profile with no gate reports 'verified' without anything having been "
                + "checked. Declare at least one IPostValidator<GeneratedArtifact> or a manifest.",
                nameof(profile));
        }

        var key = profile.TargetId.Trim();
        if (!_profiles.TryAdd(key, profile))
        {
            throw new InvalidOperationException(
                $"An agent profile for target '{key}' is already registered. "
                + "Duplicate registration is almost always a wiring bug; last-write-wins "
                + "silently masking one profile with another is never the right outcome.");
        }
    }
}
