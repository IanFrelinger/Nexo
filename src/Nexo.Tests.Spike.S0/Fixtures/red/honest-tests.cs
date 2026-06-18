using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Tests;

public sealed class ColumnTypeInferrerRedTests
{
    [Fact]
    public void Integer_column_is_inferred_as_Integer()
    {
        ColumnTypeInferrer.InferType(["1", "2", "3"]).Should().Be(ColumnType.Integer);
    }

    [Fact]
    public void Decimal_column_is_inferred_as_Decimal()
    {
        ColumnTypeInferrer.InferType(["1.5", "2.0"]).Should().Be(ColumnType.Decimal);
    }

    [Fact]
    public void Boolean_column_is_inferred_as_Boolean()
    {
        ColumnTypeInferrer.InferType(["true", "false"]).Should().Be(ColumnType.Boolean);
    }

    [Fact]
    public void Date_column_is_inferred_as_Date()
    {
        ColumnTypeInferrer.InferType(["2024-01-15"]).Should().Be(ColumnType.Date);
    }

    [Fact]
    public void Text_column_is_inferred_as_String()
    {
        ColumnTypeInferrer.InferType(["hello", "world"]).Should().Be(ColumnType.String);
    }

    [Fact]
    public void Mixed_numeric_and_text_is_String()
    {
        ColumnTypeInferrer.InferType(["1", "hello"]).Should().Be(ColumnType.String);
    }
}
