using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Tests;

/// <summary>Hollow suite: never asserts malformed boolean rejection.</summary>
public sealed class ColumnTypeInferrerHollowBooleanTests
{
    [Fact]
    public void True_false_pair_is_Boolean()
    {
        ColumnTypeInferrer.InferType(["true", "false"]).Should().Be(ColumnType.Boolean);
    }
}
