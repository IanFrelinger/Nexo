using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;
using Ashlar.Abstractions.Database;
using Ashlar.Abstractions.Transport;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>Tests for barrier level.</summary>
public class BarrierLevelTests
{
    [Fact]
    public void Constructor_rejects_blank_name()
    {
        var act = () => new BarrierLevel("  ", 1);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void CompareTo_and_operators_work()
    {
        var low = new BarrierLevel("low", 1);
        var high = new BarrierLevel("high", 5);

        low.CompareTo(high).Should().BeNegative();
        low.CompareTo(null).Should().BePositive();
        (low < high).Should().BeTrue();
        (low <= high).Should().BeTrue();
        (high > low).Should().BeTrue();
        (high >= low).Should().BeTrue();
        low.Equals(high).Should().BeFalse();
        low.GetHashCode().Should().NotBe(high.GetHashCode());
    }
}
