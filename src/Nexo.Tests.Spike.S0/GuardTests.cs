using FluentAssertions;
using Nexo.Spike.S0.Guards;
using Nexo.Spike.S0.Models;
using Xunit;

namespace Nexo.Tests.Spike.S0;

public sealed class RedReasonClassifierTests
{
    [Fact]
    public void Classify_returns_PassedAgainstStub_when_exit_zero()
    {
        RedReasonClassifier.Classify(0, "Passed!", "", false)
            .Should().Be(RedFailureReason.PassedAgainstStub);
    }

    [Fact]
    public void Classify_returns_Unimplemented_for_missing_symbol()
    {
        RedReasonClassifier.Classify(1, "", "error CS0246: The type or namespace name 'Foo' could not be found", false)
            .Should().Be(RedFailureReason.Unimplemented);
    }

    [Fact]
    public void Classify_returns_AssertionFailure_for_xunit_assert()
    {
        RedReasonClassifier.Classify(1, "Expected: Integer\nActual: String", "", false)
            .Should().Be(RedFailureReason.AssertionFailure);
    }
}

public sealed class TautologyGuardTests
{
    [Theory]
    [InlineData(RedFailureReason.PassedAgainstStub, true)]
    [InlineData(RedFailureReason.AssertionFailure, false)]
    [InlineData(RedFailureReason.Unimplemented, false)]
    public void IsTautological_matches_passed_against_stub(RedFailureReason reason, bool expected) =>
        TautologyGuard.IsTautological(reason).Should().Be(expected);
}
