using FluentAssertions;
using Nexo.GameDomain.Aesthetics;
using Nexo.GameDomain.Descriptors;
using Nexo.GameDomain.Macros;
using Nexo.GameDomain.Scoping;
using Nexo.GameDomain.Session;

namespace Nexo.Tests.GameDomain;

public class SessionExporterTests
{
    [Fact]
    public void ExportImport_RoundTrip_PreservesAllFields()
    {
        var session = new SessionState
        {
            SessionId = "session-001",
            Name = "Test Session",
            CreatedAtUtc = DateTimeOffset.Parse("2026-01-15T10:00:00Z"),
            LastModifiedAtUtc = DateTimeOffset.Parse("2026-01-15T12:30:00Z"),
            ForkedFromSessionId = "session-000",
            MaxPlayers = 16,
            TickRate = 128,
            GameRules = new GameRuleDescriptor
            {
                Id = "tdm",
                Name = "Team Deathmatch",
                Mode = "team_deathmatch",
                RoundDurationSeconds = 600,
                MaxScore = 50,
                TeamCount = 2,
                RespawnDelaySeconds = 5.0,
                FriendlyFire = false
            },
            Metadata = new Dictionary<string, string> { ["env"] = "staging", ["version"] = "2.0" }
        };

        var json = SessionExporter.ExportToJson(session);
        var restored = SessionExporter.ImportFromJson(json);

        restored.SessionId.Should().Be(session.SessionId);
        restored.Name.Should().Be(session.Name);
        restored.CreatedAtUtc.Should().Be(session.CreatedAtUtc);
        restored.LastModifiedAtUtc.Should().Be(session.LastModifiedAtUtc);
        restored.ForkedFromSessionId.Should().Be(session.ForkedFromSessionId);
        restored.MaxPlayers.Should().Be(session.MaxPlayers);
        restored.TickRate.Should().Be(session.TickRate);
        restored.GameRules.Should().NotBeNull();
        restored.GameRules!.Id.Should().Be("tdm");
        restored.GameRules.Mode.Should().Be("team_deathmatch");
        restored.Metadata.Should().HaveCount(2);
        restored.Metadata["env"].Should().Be("staging");
    }

    [Fact]
    public void ExportImport_EmptySession_RoundTrips()
    {
        var session = new SessionState();

        var json = SessionExporter.ExportToJson(session);
        var restored = SessionExporter.ImportFromJson(json);

        restored.SessionId.Should().BeEmpty();
        restored.Weapons.Should().BeEmpty();
        restored.Abilities.Should().BeEmpty();
        restored.MapElements.Should().BeEmpty();
        restored.AiBehaviors.Should().BeEmpty();
        restored.AestheticPacks.Should().BeEmpty();
        restored.Macros.Should().BeEmpty();
        restored.ScopedSettings.Should().BeEmpty();
    }

    [Fact]
    public void ExportImport_SessionWithWeaponsAbilitiesMapElementsRules_RoundTrips()
    {
        var session = new SessionState
        {
            SessionId = "full-session",
            Name = "Full Session",
            GameRules = new GameRuleDescriptor
            {
                Id = "koth",
                Name = "King of the Hill",
                Mode = "king_of_hill",
                MaxScore = 100,
                TeamCount = 2
            },
            Weapons =
            [
                new WeaponDescriptor
                {
                    Id = "blaster",
                    Name = "Plasma Blaster",
                    DamagePerHit = 25,
                    FireRatePerSecond = 8,
                    Range = 50,
                    MagazineSize = 30,
                    ReloadTimeSeconds = 2.0,
                    ProjectileType = "projectile",
                    SpecialEffects = ["burn"],
                    Tags = ["energy", "rifle"]
                }
            ],
            Abilities =
            [
                new AbilityDescriptor
                {
                    Id = "dash",
                    Name = "Dash",
                    CooldownSeconds = 5.0,
                    Duration = 0.5,
                    EnergyCost = 20,
                    EffectType = "movement",
                    Parameters = new Dictionary<string, double> { ["speed_multiplier"] = 3.0 },
                    Tags = ["movement"]
                }
            ],
            MapElements =
            [
                new MapElementDescriptor
                {
                    Id = "wall-01",
                    Name = "Steel Wall",
                    Category = "structure",
                    Dimensions = new Dimensions(4.0, 3.0, 0.5),
                    MaterialClass = "metal",
                    IsInteractive = false,
                    Tags = ["cover"]
                }
            ],
        };

        var json = SessionExporter.ExportToJson(session);
        var restored = SessionExporter.ImportFromJson(json);

        restored.Weapons.Should().HaveCount(1);
        restored.Weapons[0].Id.Should().Be("blaster");
        restored.Weapons[0].DamagePerHit.Should().Be(25);

        restored.Abilities.Should().HaveCount(1);
        restored.Abilities[0].Id.Should().Be("dash");
        restored.Abilities[0].EffectType.Should().Be("movement");

        restored.MapElements.Should().HaveCount(1);
        restored.MapElements[0].Id.Should().Be("wall-01");
        restored.MapElements[0].Dimensions.Width.Should().Be(4.0);

        restored.GameRules!.Mode.Should().Be("king_of_hill");
    }

    [Fact]
    public void ExportImport_SessionWithScopedSettingsAndMacros_RoundTrips()
    {
        var session = new SessionState
        {
            SessionId = "scoped-session",
            Name = "Scoped Session",
            ScopedSettings =
            [
                new ScopedSetting
                {
                    SettingId = "gravity",
                    Value = 9.8,
                    Scope = new SettingScope { Type = SettingScopeType.Server },
                    CreatedBy = "admin",
                    CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                }
            ],
            Macros =
            [
                new MacroDefinition
                {
                    MacroId = "loadout-preset",
                    DisplayName = "Default Loadout",
                    Description = "Spawns default weapons",
                    Author = "designer",
                    Version = "1.0.0",
                    Tags = ["loadout"],
                    Steps =
                    [
                        new MacroStep
                        {
                            Action = "spawn_weapon",
                            Args = new Dictionary<string, object> { ["weaponId"] = "blaster" }
                        }
                    ]
                }
            ]
        };

        var json = SessionExporter.ExportToJson(session);
        var restored = SessionExporter.ImportFromJson(json);

        restored.ScopedSettings.Should().HaveCount(1);
        restored.ScopedSettings[0].SettingId.Should().Be("gravity");

        restored.Macros.Should().HaveCount(1);
        restored.Macros[0].MacroId.Should().Be("loadout-preset");
        restored.Macros[0].Steps.Should().HaveCount(1);
        restored.Macros[0].Steps[0].Action.Should().Be("spawn_weapon");
    }
}
