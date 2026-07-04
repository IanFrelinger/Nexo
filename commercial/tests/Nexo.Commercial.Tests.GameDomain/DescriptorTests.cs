using System.Text.Json;
using FluentAssertions;
using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Descriptors;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for descriptor.</summary>
public class DescriptorTests
{
    [Fact]
    public void WeaponDescriptor_DefaultValues()
    {
        var weapon = new WeaponDescriptor();

        weapon.Id.Should().BeEmpty();
        weapon.Name.Should().BeEmpty();
        weapon.DamagePerHit.Should().Be(0);
        weapon.FireRatePerSecond.Should().Be(0);
        weapon.Range.Should().Be(0);
        weapon.MagazineSize.Should().Be(0);
        weapon.ReloadTimeSeconds.Should().Be(0);
        weapon.ProjectileType.Should().Be("hitscan");
        weapon.SpecialEffects.Should().BeEmpty();
        weapon.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AbilityDescriptor_DefaultValues()
    {
        var ability = new AbilityDescriptor();

        ability.Id.Should().BeEmpty();
        ability.Name.Should().BeEmpty();
        ability.CooldownSeconds.Should().Be(0);
        ability.Duration.Should().Be(0);
        ability.EnergyCost.Should().Be(0);
        ability.EffectType.Should().Be("utility");
        ability.Parameters.Should().BeEmpty();
        ability.Tags.Should().BeEmpty();
    }

    [Fact]
    public void MapElementDescriptor_DefaultValues()
    {
        var element = new MapElementDescriptor();

        element.Id.Should().BeEmpty();
        element.Name.Should().BeEmpty();
        element.Category.Should().Be("structure");
        element.Dimensions.Should().NotBeNull();
        element.Dimensions.Width.Should().Be(1.0);
        element.Dimensions.Height.Should().Be(1.0);
        element.Dimensions.Depth.Should().Be(1.0);
        element.MaterialClass.Should().Be("stone");
        element.IsInteractive.Should().BeFalse();
        element.Tags.Should().BeEmpty();
    }

    [Fact]
    public void GameRuleDescriptor_DefaultValues()
    {
        var rules = new GameRuleDescriptor();

        rules.Id.Should().BeEmpty();
        rules.Name.Should().BeEmpty();
        rules.Mode.Should().Be("deathmatch");
        rules.RoundDurationSeconds.Should().Be(300);
        rules.MaxScore.Should().Be(25);
        rules.TeamCount.Should().Be(1);
        rules.RespawnDelaySeconds.Should().Be(3.0);
        rules.FriendlyFire.Should().BeFalse();
        rules.CustomProperties.Should().BeEmpty();
    }

    [Fact]
    public void AiBehaviorDescriptor_DefaultValues()
    {
        var ai = new AiBehaviorDescriptor();

        ai.Id.Should().BeEmpty();
        ai.Name.Should().BeEmpty();
        ai.Archetype.Should().Be("patrol");
        ai.Aggression.Should().Be(0.5);
        ai.Accuracy.Should().Be(0.5);
        ai.ReactionTimeSeconds.Should().Be(0.3);
        ai.PreferredRange.Should().Be(15.0);
        ai.Tags.Should().BeEmpty();
    }

    [Fact]
    public void WeaponDescriptor_JsonRoundTrip()
    {
        var original = new WeaponDescriptor
        {
            Id = "railgun",
            Name = "Railgun",
            DamagePerHit = 100,
            FireRatePerSecond = 0.5,
            Range = 200,
            MagazineSize = 1,
            ReloadTimeSeconds = 3.0,
            ProjectileType = "hitscan",
            SpecialEffects = ["pierce"],
            Tags = ["sniper", "heavy"]
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        var json = JsonSerializer.Serialize(original, options);
        var restored = JsonSerializer.Deserialize<WeaponDescriptor>(json, options);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Name.Should().Be(original.Name);
        restored.DamagePerHit.Should().Be(original.DamagePerHit);
        restored.FireRatePerSecond.Should().Be(original.FireRatePerSecond);
        restored.Range.Should().Be(original.Range);
        restored.MagazineSize.Should().Be(original.MagazineSize);
        restored.ReloadTimeSeconds.Should().Be(original.ReloadTimeSeconds);
        restored.ProjectileType.Should().Be(original.ProjectileType);
        restored.SpecialEffects.Should().BeEquivalentTo(original.SpecialEffects);
        restored.Tags.Should().BeEquivalentTo(original.Tags);
    }

    [Fact]
    public void AestheticPack_WithLodLevels()
    {
        var pack = new AestheticPack
        {
            Id = "neon-glow",
            Name = "Neon Glow",
            GeometryStrategy = "low_poly",
            DefaultPaletteColors = ["#FF00FF", "#00FFFF", "#FFFF00"],
            LodLevels =
            [
                new LodLevel(0, 1.0),
                new LodLevel(1, 0.5),
                new LodLevel(2, 0.25),
            ],
            PostProcessEffects = ["bloom", "chromatic_aberration"]
        };

        pack.Id.Should().Be("neon-glow");
        pack.GeometryStrategy.Should().Be("low_poly");
        pack.DefaultPaletteColors.Should().HaveCount(3);
        pack.LodLevels.Should().HaveCount(3);
        pack.LodLevels[0].Level.Should().Be(0);
        pack.LodLevels[0].DetailFactor.Should().Be(1.0);
        pack.LodLevels[2].DetailFactor.Should().Be(0.25);
        pack.PostProcessEffects.Should().Contain("bloom");
    }

    [Fact]
    public void Dimensions_RecordEquality()
    {
        var a = new Dimensions(2.0, 3.0, 4.0);
        var b = new Dimensions(2.0, 3.0, 4.0);
        var c = new Dimensions(1.0, 3.0, 4.0);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
    }

    [Fact]
    public void Dimensions_DefaultValues()
    {
        var d = new Dimensions();

        d.Width.Should().Be(1.0);
        d.Height.Should().Be(1.0);
        d.Depth.Should().Be(1.0);
    }
}
