using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Tests;

/// <summary>Hollow suite: no order-independence / permutation coverage.</summary>
public sealed class ColumnTypeInferrerHollowOrderTests
{
    [Fact]
    public void Mixed_column_is_String_in_one_order()
    {
        ColumnTypeInferrer.InferType(["1", "2", "hello"]).Should().Be(ColumnType.String);
    }
}
