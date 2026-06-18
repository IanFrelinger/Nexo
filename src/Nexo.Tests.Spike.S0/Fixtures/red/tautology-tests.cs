using FluentAssertions;
using CsvColumnInferrer;
using Xunit;

namespace CsvColumnInferrer.Tests;

public sealed class ColumnTypeInferrerTautologyTests
{
    [Fact]
    public void InferType_is_reflexive()
    {
        var values = new[] { "1", "2" };
        ColumnTypeInferrer.InferType(values).Should().Be(ColumnTypeInferrer.InferType(values));
    }
}
