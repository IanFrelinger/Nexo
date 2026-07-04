using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier hierarchy gap coverage.</summary>
public sealed class BarrierHierarchyGapCoverageTests
{
    private static readonly BarrierHierarchy Hierarchy = new([
        new BarrierLevel("public", 0),
        new BarrierLevel("internal", 1),
        new BarrierLevel("confidential", 2),
    ]);

    [Fact]
    public void Floor_and_ceiling_return_ordered_extremes()
    {
        Hierarchy.Floor.Name.Should().Be("public");
        Hierarchy.Ceiling.Name.Should().Be("confidential");
    }

    [Fact]
    public void IsKnown_rejects_blank_names()
    {
        Hierarchy.IsKnown("").Should().BeFalse();
        Hierarchy.IsKnown("   ").Should().BeFalse();
    }

    [Fact]
    public void Get_throws_for_unknown_level()
    {
        var act = () => Hierarchy.Get("missing");

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void IsAtOrBelow_and_IsAbove_compare_ranks()
    {
        Hierarchy.IsAtOrBelow("public", "internal").Should().BeTrue();
        Hierarchy.IsAtOrBelow("confidential", "internal").Should().BeFalse();
        Hierarchy.IsAbove("confidential", "public").Should().BeTrue();
        Hierarchy.IsAbove("public", "internal").Should().BeFalse();
    }

    [Fact]
    public void Highest_returns_higher_ranked_level()
    {
        Hierarchy.Highest("public", "confidential").Name.Should().Be("confidential");
        Hierarchy.Highest("internal", "internal").Name.Should().Be("internal");
    }

    [Fact]
    public void Enumerator_yields_level_names_in_rank_order()
    {
        Hierarchy.ToList().Should().Equal("public", "internal", "confidential");
    }
}
