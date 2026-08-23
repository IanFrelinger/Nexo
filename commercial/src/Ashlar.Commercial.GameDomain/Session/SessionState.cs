using Ashlar.Commercial.GameDomain.Aesthetics;
using Ashlar.Commercial.GameDomain.Descriptors;
using Ashlar.Commercial.GameDomain.Macros;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.GameDomain.Session;
/// <summary>
/// Represents the full state of a Ashlar Forge prototyping session.
/// </summary>
public sealed class SessionState
{
    /// <summary>Stable session identifier.</summary>
    public string SessionId { get; set; } = string.Empty;
    /// <summary>Human-readable session name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>UTC timestamp when the session was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>UTC timestamp of the last session mutation.</summary>
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    /// <summary>Source session id when this session was forked.</summary>
    public string? ForkedFromSessionId { get; set; }

    /// <summary>Active game rule descriptor for the session.</summary>
    public GameRuleDescriptor? GameRules { get; set; }
    /// <summary>Maximum supported player count.</summary>
    public int MaxPlayers { get; set; }
    /// <summary>Simulation tick rate in hertz.</summary>
    public int TickRate { get; set; } = 64;

    /// <summary>Weapon descriptors authored in this session.</summary>
    public List<WeaponDescriptor> Weapons { get; set; } = [];
    /// <summary>Map element descriptors authored in this session.</summary>
    public List<MapElementDescriptor> MapElements { get; set; } = [];
    /// <summary>Ability descriptors authored in this session.</summary>
    public List<AbilityDescriptor> Abilities { get; set; } = [];
    /// <summary>AI behavior descriptors authored in this session.</summary>
    public List<AiBehaviorDescriptor> AiBehaviors { get; set; } = [];
    /// <summary>Aesthetic packs applied to this session.</summary>
    public List<AestheticPack> AestheticPacks { get; set; } = [];
    /// <summary>Macro definitions available in this session.</summary>
    public List<MacroDefinition> Macros { get; set; } = [];
    /// <summary>Scoped settings resolved for this session.</summary>
    public List<ScopedSetting> ScopedSettings { get; set; } = [];

    /// <summary>Arbitrary session metadata key-value pairs.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
