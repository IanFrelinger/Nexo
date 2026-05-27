using FluentAssertions;
using Nexo.Runtime.Barriers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Barriers;

public sealed class BarrierCeilingExceededExceptionGapCoverageTests
{
    [Fact]
    public void Constructor_populates_properties_and_error_code()
    {
        var ex = new BarrierCeilingExceededException("internal", "public", "corr-99");

        ex.Message.Should().Contain("internal");
        ex.Message.Should().Contain("public");
        ex.RequestedLevel.Should().Be("internal");
        ex.HostCeiling.Should().Be("public");
        ex.CorrelationId.Should().Be("corr-99");
        ex.ErrorCode.Should().Be("BARRIER_CEILING_EXCEEDED");
    }
}
