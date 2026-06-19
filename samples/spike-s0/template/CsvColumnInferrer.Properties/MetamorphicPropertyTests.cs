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

    public static TheoryData<string[]> WhitespaceInvariantBaselines => new()
    {
        { ["1", "2", "3"] },
        { ["hello", "world"] },
        { ["true", "false"] },
        { ["1", "hello"] },
        { ["2024-01-15"] }
    };

    /// <summary>
    /// Metamorphic invariant: inserting whitespace-only cells does not change inferred type.
    /// </summary>
    [Theory]
    [MemberData(nameof(WhitespaceInvariantBaselines))]
    public void InferType_is_invariant_when_inserting_whitespace_only_cells(string[] baseline)
    {
        var expected = ColumnTypeInferrer.InferType(baseline);
        var interleaved = baseline.SelectMany(v => new[] { "   ", v }).ToList();
        ColumnTypeInferrer.InferType(interleaved)
            .Should().Be(expected, because: "whitespace-only cells treated as empty");
        var trailing = baseline.Concat(["   ", "\t"]).ToList();
        ColumnTypeInferrer.InferType(trailing)
            .Should().Be(expected, because: "whitespace-only cells treated as empty");
    }

    [Fact]
    public void Whitespace_only_cells_are_treated_as_empty()
    {
        ColumnTypeInferrer.InferType(["   "]).Should().Be(ColumnType.String);
        ColumnTypeInferrer.InferType(["\t", "  "]).Should().Be(ColumnType.String);
    }
}
