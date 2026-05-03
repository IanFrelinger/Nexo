using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Session;

namespace Nexo.API.Forge;

/// <summary>
/// In-memory Forge state (default). Suitable for tests and single-process prototyping.
/// </summary>
public sealed class InMemoryForgeStateService : IForgeStateService
{
    private SessionState _session = CreateDefaultSession();
    private MacroRegistry _registry = new();

    public SessionState Session
    {
        get => _session;
        set => _session = value ?? throw new ArgumentNullException(nameof(value));
    }

    public MacroRegistry Registry
    {
        get => _registry;
        set => _registry = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void Save()
    {
        // No durable backing store.
    }

    /// <summary>
    /// Resets session and macro registry (for tests).
    /// </summary>
    public void Reset()
    {
        _session = CreateDefaultSession();
        _registry = new MacroRegistry();
    }

    internal static SessionState CreateDefaultSession()
    {
        return new SessionState
        {
            SessionId = Guid.NewGuid().ToString("D"),
            Name = "Default Forge Session",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            MaxPlayers = 8,
            GameRules = new Nexo.GameDomain.Descriptors.GameRuleDescriptor
            {
                Id = Guid.NewGuid().ToString("D"),
                Name = "Default",
                Mode = "deathmatch"
            }
        };
    }
}
