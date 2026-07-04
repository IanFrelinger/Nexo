using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;
using Nexo.Abstractions.Database;
using Nexo.Abstractions.Transport;
using Xunit;

namespace Nexo.Tests.Kernel;

/// <summary>Tests for barrier hierarchy.</summary>
public class BarrierHierarchyTests
{
    /// <summary>Two level.</summary>
    private static BarrierHierarchy TwoLevel() => new(new[]
    {
        new BarrierLevel("public", 0),
        new BarrierLevel("secret", 10),
    });

    [Fact]
    public void Constructor_orders_by_rank_and_exposes_floor_ceiling()
    {
        var h = TwoLevel();
        h.Floor.Name.Should().Be("public");
        h.Ceiling.Name.Should().Be("secret");
        h.Select(x => x).Should().Equal("public", "secret");
    }

    [Fact]
    public void Non_generic_IEnumerable_enumerates_level_names()
    {
        var h = TwoLevel();
        var names = new List<string>();
        System.Collections.IEnumerable untyped = h;
        foreach (string name in untyped) names.Add(name);
        names.Should().Equal("public", "secret");
    }

    [Fact]
    public void Constructor_validates_input()
    {
        Assert.Throws<ArgumentNullException>(() => new BarrierHierarchy(null!));
        Assert.Throws<ArgumentException>(() => new BarrierHierarchy(Array.Empty<BarrierLevel>()));
        Assert.Throws<ArgumentException>(() => new BarrierHierarchy(new[]
        {
            new BarrierLevel("a", 1),
            new BarrierLevel("A", 2),
        }));
        Assert.Throws<ArgumentException>(() => new BarrierHierarchy(new[]
        {
            new BarrierLevel("a", 1),
            new BarrierLevel("b", 1),
        }));
    }

    [Fact]
    public void Get_IsKnown_IsAtOrBelow_IsAbove_Highest()
    {
        var h = TwoLevel();
        h.IsKnown("public").Should().BeTrue();
        h.IsKnown("PUBLIC").Should().BeTrue();
        h.IsKnown("").Should().BeFalse();
        h.IsKnown("missing").Should().BeFalse();

        h.Get("secret").Rank.Should().Be(10);
        Assert.Throws<ArgumentException>(() => h.Get(""));
        Assert.Throws<ArgumentException>(() => h.Get("nope"));

        h.IsAtOrBelow("public", "secret").Should().BeTrue();
        h.IsAtOrBelow("secret", "public").Should().BeFalse();
        h.IsAbove("secret", "public").Should().BeTrue();
        h.Highest("public", "secret").Name.Should().Be("secret");
        h.Highest("secret", "public").Name.Should().Be("secret");
    }
}
