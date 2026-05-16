using FluentAssertions;
using Nexo.GameDomain.Scoping;

namespace Nexo.Tests.GameDomain;

public class ScopeResolverTests
{
    private static ScopedSetting MakeSetting(
        string settingId,
        object value,
        SettingScopeType type,
        string? target = null,
        DateTimeOffset? expiresAt = null)
    {
        return new ScopedSetting
        {
            SettingId = settingId,
            Value = value,
            Scope = new SettingScope { Type = type, Target = target },
            CreatedBy = "test",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    [Fact]
    public void Resolve_NarrowestWins_MomentBeatsPlayerBeatsZoneBeatsTeamBeatsServer()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("gravity", 9.8, SettingScopeType.Server),
            MakeSetting("gravity", 5.0, SettingScopeType.Team, "teamA"),
            MakeSetting("gravity", 3.0, SettingScopeType.Zone, "zoneX"),
            MakeSetting("gravity", 1.5, SettingScopeType.Player, "player1"),
            MakeSetting("gravity", 0.0, SettingScopeType.Moment, "overtime"),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext
        {
            PlayerId = "player1",
            TeamId = "teamA",
            ZoneId = "zoneX",
            ActiveMoment = "overtime"
        };

        var result = resolver.Resolve("gravity", context);

        result.Should().Be(0.0);
    }

    [Fact]
    public void Resolve_MissingScopeFallsThrough_ToBroaderScope()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("gravity", 9.8, SettingScopeType.Server),
            MakeSetting("gravity", 5.0, SettingScopeType.Team, "teamA"),
            MakeSetting("gravity", 3.0, SettingScopeType.Zone, "zoneX"),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext
        {
            PlayerId = "player1",
            TeamId = "teamA",
            ZoneId = "zoneX",
        };

        var result = resolver.Resolve("gravity", context);

        result.Should().Be(3.0, "zone is narrower than team and server, and player/moment are absent");
    }

    [Fact]
    public void Resolve_ExpiredMomentSetting_IsSkipped()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("speed", 10.0, SettingScopeType.Server),
            MakeSetting("speed", 99.0, SettingScopeType.Moment, "boost",
                expiresAt: DateTimeOffset.UtcNow.AddHours(-1)),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext { ActiveMoment = "boost" };

        var result = resolver.Resolve("speed", context);

        result.Should().Be(10.0, "the moment setting has expired and should be skipped");
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoMatch()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("gravity", 9.8, SettingScopeType.Player, "player2"),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext { PlayerId = "player1" };

        var result = resolver.Resolve("gravity", context);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveAll_ReturnsAllSettingsForContext()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("gravity", 9.8, SettingScopeType.Server),
            MakeSetting("speed", 20.0, SettingScopeType.Player, "player1"),
            MakeSetting("damage", 1.5, SettingScopeType.Team, "teamA"),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext
        {
            PlayerId = "player1",
            TeamId = "teamA"
        };

        var result = resolver.ResolveAll(context);

        result.Should().HaveCount(3);
        result["gravity"].Should().Be(9.8);
        result["speed"].Should().Be(20.0);
        result["damage"].Should().Be(1.5);
    }

    [Fact]
    public void Resolve_ServerOnlySettings_Work()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("time_scale", 1.0, SettingScopeType.Server),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext();

        var result = resolver.Resolve("time_scale", context);

        result.Should().Be(1.0);
    }

    [Fact]
    public void ResolveAll_MultipleScopesCompose_EachSettingGetsNarrowestMatch()
    {
        var settings = new List<ScopedSetting>
        {
            MakeSetting("gravity", 9.8, SettingScopeType.Server),
            MakeSetting("gravity", 5.0, SettingScopeType.Player, "player1"),
            MakeSetting("speed", 10.0, SettingScopeType.Server),
            MakeSetting("speed", 30.0, SettingScopeType.Team, "teamA"),
            MakeSetting("health", 100.0, SettingScopeType.Server),
        };

        var resolver = new ScopeResolver(settings);
        var context = new ScopeContext
        {
            PlayerId = "player1",
            TeamId = "teamA"
        };

        var result = resolver.ResolveAll(context);

        result.Should().HaveCount(3);
        result["gravity"].Should().Be(5.0, "player scope is narrower than server");
        result["speed"].Should().Be(30.0, "team scope is narrower than server");
        result["health"].Should().Be(100.0, "only server scope exists");
    }
}
