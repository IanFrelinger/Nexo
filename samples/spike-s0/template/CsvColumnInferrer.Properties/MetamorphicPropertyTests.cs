using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Properties;

/// <summary>
/// Load-bearing metamorphic property: column inference is independent of cell order.
/// </summary>
[Trait("Category", "SpikeProperty")]
public sealed class MetamorphicPropertyTests
{
    public static TheoryData<string[]> OrderPairs => new()
    {
        { ["1", "2", "hello"] },
        { ["hello", "1", "2"] },
        { ["1", "hello", "2"] },
        { ["true", "false"] },
        { ["false", "true"] },
        { ["1", "2", "3"] },
        { ["3", "2", "1"] }
    };

    [Theory]
    [MemberData(nameof(OrderPairs))]
    public void InferType_is_invariant_under_value_permutation(string[] values)
    {
        var baseline = ColumnTypeInferrer.InferType(values);
        var reversed = values.Reverse().ToList();
        ColumnTypeInferrer.InferType(reversed).Should().Be(baseline);
        if (values.Length >= 3)
        {
            var rotated = new[] { values[1], values[2], values[0] };
            ColumnTypeInferrer.InferType(rotated).Should().Be(baseline);
        }
    }

    [Fact]
    public void InferType_is_deterministic()
    {
        var values = new[] { "1", "hello", "true" };
        ColumnTypeInferrer.InferType(values).Should().Be(ColumnTypeInferrer.InferType(values));
    }
}
