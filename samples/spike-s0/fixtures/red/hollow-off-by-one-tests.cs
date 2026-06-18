using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Tests;

/// <summary>Hollow suite: misses single-value boundary (off-by-one adversarial).</summary>
public sealed class ColumnTypeInferrerHollowBoundaryTests
{
    [Fact]
    public void Multi_value_integers_are_Integer()
    {
        ColumnTypeInferrer.InferType(["1", "2", "3"]).Should().Be(ColumnType.Integer);
    }
}
