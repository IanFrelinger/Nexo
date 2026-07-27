using System.Collections.Concurrent;
using Nexo.Core.Application.Generation.Ports;
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
        _profiles[profile.TargetId.Trim()] = profile;
    }
}
