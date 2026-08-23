using Ashlar.Commercial.GameDomain.Descriptors;
using Ashlar.Commercial.GameDomain.Macros;
using Ashlar.Commercial.GameDomain.Session;

namespace GameDirector.Mcp.Forge;
/// <summary>Service for in memory forge state operations.</summary>
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
    }

    public void Reset()
    {
        _session = CreateDefaultSession();
        _registry = new MacroRegistry();
    }

    /// <summary>Creates default session.</summary>
    internal static SessionState CreateDefaultSession() => new()
    {
        SessionId = Guid.NewGuid().ToString("D"),
        Name = "Default Forge Session",
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastModifiedAtUtc = DateTimeOffset.UtcNow,
        MaxPlayers = 8,
        GameRules = new GameRuleDescriptor
        {
            Id = Guid.NewGuid().ToString("D"),
            Name = "Default",
            Mode = "deathmatch"
        }
    };
}
