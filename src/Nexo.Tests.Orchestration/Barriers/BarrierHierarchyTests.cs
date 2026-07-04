using FluentAssertions;
using Nexo.Abstractions.Barriers;
using Xunit;

namespace Nexo.Tests.Orchestration.Barriers;

/// <summary>Tests for barrier hierarchy.</summary>
public sealed class BarrierHierarchyTests
{
    [Fact]
    public void Matrix_IsAtOrBelow_And_IsAbove_WorksForLowMediumHigh()
    {
        var hierarchy = CreateHierarchy("low", "medium", "high");

        hierarchy.IsAtOrBelow("low", "low").Should().BeTrue();
        hierarchy.IsAtOrBelow("low", "medium").Should().BeTrue();
        hierarchy.IsAtOrBelow("low", "high").Should().BeTrue();
        hierarchy.IsAtOrBelow("medium", "low").Should().BeFalse();
        hierarchy.IsAtOrBelow("medium", "medium").Should().BeTrue();
        hierarchy.IsAtOrBelow("medium", "high").Should().BeTrue();
        hierarchy.IsAtOrBelow("high", "low").Should().BeFalse();
        hierarchy.IsAtOrBelow("high", "medium").Should().BeFalse();
        hierarchy.IsAtOrBelow("high", "high").Should().BeTrue();

        hierarchy.IsAbove("low", "low").Should().BeFalse();
        hierarchy.IsAbove("low", "medium").Should().BeFalse();
        hierarchy.IsAbove("low", "high").Should().BeFalse();
        hierarchy.IsAbove("medium", "low").Should().BeTrue();
        hierarchy.IsAbove("medium", "medium").Should().BeFalse();
        hierarchy.IsAbove("medium", "high").Should().BeFalse();
        hierarchy.IsAbove("high", "low").Should().BeTrue();
        hierarchy.IsAbove("high", "medium").Should().BeTrue();
        hierarchy.IsAbove("high", "high").Should().BeFalse();
    }

    [Fact]
    public void Constructor_DuplicateNames_Throws()
    {
        var act = () => new BarrierHierarchy([
            new BarrierLevel("low", 0),
            new BarrierLevel("low", 1)
        ]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_DuplicateRanks_Throws()
    {
        var act = () => new BarrierHierarchy([
            new BarrierLevel("low", 0),
            new BarrierLevel("medium", 0)
        ]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ZeroLevels_Throws()
    {
        var act = () => new BarrierHierarchy(Array.Empty<BarrierLevel>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FloorAndCeiling_ReturnCorrectLevels()
    {
        var hierarchy = CreateHierarchy("a", "b", "c");

        hierarchy.Floor.Name.Should().Be("a");
        hierarchy.Ceiling.Name.Should().Be("c");
    }

    private static BarrierHierarchy CreateHierarchy(params string[] names)
    {
        /// <summary>Barrier hierarchy.</summary>
        return new BarrierHierarchy(names.Select((name, rank) => new BarrierLevel(name, rank)));
    }
}
