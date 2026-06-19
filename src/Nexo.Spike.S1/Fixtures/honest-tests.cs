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

    // S1.3 pin: zero-one-literals — ["0","1"] => Integer (parses as int; not Boolean)
    [Fact]
    public void Zero_one_literals_are_Integer_not_Boolean()
    {
        ColumnTypeInferrer.InferType(["0", "1"]).Should().Be(ColumnType.Integer);
    }

    // S1.3 pin: leading-zeros — ["007"] => Integer
    [Fact]
    public void Leading_zero_numerics_are_Integer()
    {
        ColumnTypeInferrer.InferType(["007"]).Should().Be(ColumnType.Integer);
    }

    // S1.3 pin: thousands-separator — ["1,000"] => Decimal
    [Fact]
    public void Thousands_separated_numerics_are_Decimal()
    {
        ColumnTypeInferrer.InferType(["1,000"]).Should().Be(ColumnType.Decimal);
    }

    // S1.3 pin: locale-comma-decimal — ["1,5"] => Date; edge ["1,."] => Decimal
    [Fact]
    public void Locale_comma_decimal_literals_are_Date()
    {
        ColumnTypeInferrer.InferType(["1,5"]).Should().Be(ColumnType.Date);
    }

    [Fact]
    public void Locale_comma_malformed_decimal_edge_is_Decimal()
    {
        ColumnTypeInferrer.InferType(["1,."]).Should().Be(ColumnType.Decimal);
    }

    // S1.3 pin: signed-zero — ["+0","-0"] => Integer
    [Fact]
    public void Signed_zero_literals_are_Integer()
    {
        ColumnTypeInferrer.InferType(["+0", "-0"]).Should().Be(ColumnType.Integer);
    }

    // S1.3 pin: boolean-yes-no — ["yes","no"] => String (not Boolean)
    [Fact]
    public void Yes_no_tokens_are_String_not_Boolean()
    {
        ColumnTypeInferrer.InferType(["yes", "no"]).Should().Be(ColumnType.String);
    }

    // S1.3 pin: boolean-yn — ["Y","N"] => String (not Boolean)
    [Fact]
    public void Y_N_tokens_are_String_not_Boolean()
    {
        ColumnTypeInferrer.InferType(["Y", "N"]).Should().Be(ColumnType.String);
    }

    // S1.3 pin: whitespace-only — filtered as empty => String
    [Fact]
    public void Whitespace_only_cells_are_String()
    {
        ColumnTypeInferrer.InferType(["   "]).Should().Be(ColumnType.String);
    }

    // S1.4 pin: sampling-window — ["1","2","hello"] => String (full column, not prefix)
    [Fact]
    public void Prefix_integers_with_suffix_text_is_String()
    {
        ColumnTypeInferrer.InferType(["1", "2", "hello"]).Should().Be(ColumnType.String);
    }
}
