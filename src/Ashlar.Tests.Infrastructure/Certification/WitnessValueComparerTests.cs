using FluentAssertions;
using Ashlar.Infrastructure.Certification;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Certification;

/// <summary>
/// The correctness gate proves EXACT witnessed behavior, so its value comparer must not coerce
/// across type kinds. These pin the holes a white-box lab pass found: an integer witness silently
/// matching a double that rounds to it, or matching a numeric string or a bool. A brick whose real
/// output is wrong-by-&lt;0.5 or wrong-typed must FAIL the witness, not pass it.
/// </summary>
public sealed class WitnessValueComparerTests
{
    [Theory]
    [InlineData(2, 2.4)]     // rounds down to 2
    [InlineData(2, 2.5)]     // banker's rounding to even → 2
    [InlineData(2, 1.5)]     // rounds up to 2
    [InlineData(2, 1.6)]
    public void An_integer_witness_does_not_match_a_nearby_double(int expected, double actual) =>
        WitnessValueComparer.AreEqual(expected, actual).Should().BeFalse(
            $"a witness expecting int {expected} must not accept double {actual}");

    [Fact]
    public void An_integer_witness_does_not_match_a_numeric_string()
    {
        WitnessValueComparer.AreEqual(1, "1").Should().BeFalse();
        WitnessValueComparer.AreEqual("1", 1).Should().BeFalse();
    }

    [Fact]
    public void An_integer_witness_does_not_match_a_boolean()
    {
        WitnessValueComparer.AreEqual(1, true).Should().BeFalse();
        WitnessValueComparer.AreEqual(0, false).Should().BeFalse();
        WitnessValueComparer.AreEqual(true, 1).Should().BeFalse();
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(0, 0)]
    [InlineData(-7, -7)]
    public void Same_valued_integers_still_match(int expected, int actual) =>
        WitnessValueComparer.AreEqual(expected, actual).Should().BeTrue();

    [Fact]
    public void Same_valued_integers_across_widths_still_match() =>
        WitnessValueComparer.AreEqual(42, 42L).Should().BeTrue("int and long of the same value are both integral");

    [Fact]
    public void Same_strings_and_bools_and_doubles_still_match()
    {
        WitnessValueComparer.AreEqual("hello", "hello").Should().BeTrue();
        WitnessValueComparer.AreEqual(true, true).Should().BeTrue();
        WitnessValueComparer.AreEqual(false, true).Should().BeFalse();
        WitnessValueComparer.AreEqual(3.14, 3.14).Should().BeTrue();
        WitnessValueComparer.AreEqual(3.14, 3.15).Should().BeFalse();
    }
}
