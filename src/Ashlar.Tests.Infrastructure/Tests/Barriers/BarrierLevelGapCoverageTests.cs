using FluentAssertions;
using Ashlar.Abstractions.Barriers;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for barrier level gap coverage.</summary>
public sealed class BarrierLevelGapCoverageTests
{
    [Fact]
    public void Constructor_rejects_blank_name()
    {
        var act = () => new BarrierLevel("  ", 1);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void CompareTo_treats_null_as_less_than_any_level()
    {
        var level = new BarrierLevel("public", 0);

        level.CompareTo(null).Should().Be(1);
    }

    [Fact]
    public void Equality_is_based_on_rank_not_name()
    {
        var left = new BarrierLevel("public", 1);
        var right = new BarrierLevel("internal", 1);

        left.Equals(right).Should().BeTrue();
        (left == right).Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(1, 1, false)]
    [InlineData(2, 1, false)]
    public void Comparison_operators_use_rank(int leftRank, int rightRank, bool expectLessThan)
    {
        var left = new BarrierLevel("left", leftRank);
        var right = new BarrierLevel("right", rightRank);

        (left < right).Should().Be(expectLessThan);
        (left <= right).Should().Be(leftRank <= rightRank);
        (left > right).Should().Be(leftRank > rightRank);
        (left >= right).Should().Be(leftRank >= rightRank);
    }
}
