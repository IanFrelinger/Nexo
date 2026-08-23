using FluentAssertions;
using Ashlar.Commercial.GameDomain.Scoping;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for scoped setting.</summary>
public class ScopedSettingTests
{
    [Fact]
    public void ScopedSetting_DefaultValues()
    {
        var setting = new ScopedSetting();

        setting.SettingId.Should().BeEmpty();
        setting.Value.Should().BeNull();
        setting.Scope.Should().NotBeNull();
        setting.Scope.Type.Should().Be(SettingScopeType.Server);
        setting.Scope.Target.Should().BeNull();
        setting.CreatedBy.Should().BeEmpty();
        setting.CreatedAt.Should().Be(default(DateTimeOffset));
        setting.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void SettingScope_AllScopeTypes_Exist()
    {
        var allTypes = Enum.GetValues<SettingScopeType>();

        allTypes.Should().Contain(SettingScopeType.Server);
        allTypes.Should().Contain(SettingScopeType.Team);
        allTypes.Should().Contain(SettingScopeType.Zone);
        allTypes.Should().Contain(SettingScopeType.Object);
        allTypes.Should().Contain(SettingScopeType.Player);
        allTypes.Should().Contain(SettingScopeType.Moment);

        allTypes.Should().HaveCount(6, "scope hierarchy has exactly six tiers");
    }

    [Fact]
    public void ScopedSetting_WithExpiration_TracksCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddHours(2);

        var setting = new ScopedSetting
        {
            SettingId = "speed_boost",
            Value = 2.0,
            Scope = new SettingScope { Type = SettingScopeType.Moment, Target = "overtime" },
            CreatedBy = "event-system",
            CreatedAt = now,
            ExpiresAt = expiry
        };

        setting.ExpiresAt.Should().NotBeNull();
        setting.ExpiresAt!.Value.Should().BeAfter(now);
        setting.ExpiresAt.Value.Should().Be(expiry);
        setting.Scope.Type.Should().Be(SettingScopeType.Moment);
        setting.CreatedBy.Should().Be("event-system");

        var expiredSetting = setting with { ExpiresAt = now.AddHours(-1) };
        expiredSetting.ExpiresAt!.Value.Should().BeBefore(now);
    }
}
