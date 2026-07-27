using FluentAssertions;
using Nexo.Bricks.DepExtract.Gui;
using Xunit;

namespace Nexo.Bricks.DepExtract.Gui.Tests;

public sealed class OracleEvaluatorTests
{
    [Fact]
    public void DefaultMinEvents_is_at_least_5()
    {
        OracleEvaluator.DefaultMinEvents.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void Multi_sample_requires_all_to_clear_bar()
    {
        var csvGood = "ts,value\n2020-01-01T00:00:00Z,1\n2020-01-01T00:00:01Z,2\n2020-01-01T00:00:02Z,3\n2020-01-01T00:00:03Z,4\n2020-01-01T00:00:04Z,5\n";
        var v = OracleEvaluator.Evaluate(
            new[]
            {
                ("a.bin", "rows=5", csvGood),
                ("b.bin", "rows=2", csvGood), // row parse from smoke says 2 — fails min
            },
            minEvents: 5);
        v.AllPassed.Should().BeFalse();
    }

    [Fact]
    public void Field_sanity_rejects_non_monotonic_timestamps()
    {
        var csv = "timestamp,value\n2020-01-01T00:00:02Z,1\n2020-01-01T00:00:01Z,2\n2020-01-01T00:00:03Z,3\n2020-01-01T00:00:04Z,4\n2020-01-01T00:00:05Z,5\n";
        var sanity = OracleEvaluator.CheckFieldSanity(csv, bandMin: 5, bandMax: null);
        sanity.Ok.Should().BeFalse();
        sanity.Detail.Should().Contain("monotonic");
    }

    [Fact]
    public void Passing_multi_sample_with_field_sanity()
    {
        var csv = "SystemTime,value\n2020-01-01T00:00:00Z,1\n2020-01-01T00:00:01Z,2\n2020-01-01T00:00:02Z,3\n2020-01-01T00:00:03Z,4\n2020-01-01T00:00:04Z,5\n";
        var v = OracleEvaluator.Evaluate(
            new[]
            {
                ("a.bin", "rows=5", csv),
                ("b.bin", "rows=5", csv),
            },
            minEvents: 5);
        v.AllPassed.Should().BeTrue(v.Summary);
    }
}
