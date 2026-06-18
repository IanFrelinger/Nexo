using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Tests;

public sealed class ColumnTypeInferrerRedTests
{
    [Fact]
    public void Integer_column_is_not_inferred_as_String()
    {
        var result = ColumnTypeInferrer.InferType(["1", "2", "3"]);
        result.Should().Be(ColumnType.Integer);
    }
}
