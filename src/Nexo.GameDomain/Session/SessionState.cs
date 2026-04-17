using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Descriptors;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Scoping;

namespace Nexo.GameDomain.Session;

/// <summary>
/// Represents the full state of a Nexo Forge prototyping session.
/// </summary>
public sealed class SessionState
{
    public string SessionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public string? ForkedFromSessionId { get; set; }

    public GameRuleDescriptor? GameRules { get; set; }
    public int MaxPlayers { get; set; }
    public int TickRate { get; set; } = 64;

    public List<WeaponDescriptor> Weapons { get; set; } = [];
    public List<MapElementDescriptor> MapElements { get; set; } = [];
    public List<AbilityDescriptor> Abilities { get; set; } = [];
    public List<AiBehaviorDescriptor> AiBehaviors { get; set; } = [];
    public List<AestheticPack> AestheticPacks { get; set; } = [];
    public List<MacroDefinition> Macros { get; set; } = [];
    public List<ScopedSetting> ScopedSettings { get; set; } = [];

    public Dictionary<string, string> Metadata { get; set; } = new();
}
