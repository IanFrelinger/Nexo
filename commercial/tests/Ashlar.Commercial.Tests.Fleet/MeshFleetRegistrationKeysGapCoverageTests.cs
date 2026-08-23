using FluentAssertions;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh fleet registration keys gap coverage.</summary>
public sealed class MeshFleetRegistrationKeysGapCoverageTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Fingerprint_returns_null_for_blank_key(string? key)
    {
        MeshFleetRegistrationKeys.Fingerprint(key).Should().BeNull();
    }

    [Fact]
    public void Fingerprint_is_deterministic_and_truncated_to_sixteen_chars()
    {
        var first = MeshFleetRegistrationKeys.Fingerprint("peer-registration-key-123");
        var second = MeshFleetRegistrationKeys.Fingerprint("  peer-registration-key-123  ");

        first.Should().NotBeNull();
        first.Should().Be(second);
        first!.Length.Should().Be(16);
    }

    [Fact]
    public void IsDistinctFromDirectorKey_returns_true_when_director_key_missing()
    {
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("peer-key", null).Should().BeTrue();
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("peer-key", "   ").Should().BeTrue();
    }

    [Fact]
    public void IsDistinctFromDirectorKey_compares_trimmed_values()
    {
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey(" same ", "same").Should().BeFalse();
        MeshFleetRegistrationKeys.IsDistinctFromDirectorKey("peer", "director").Should().BeTrue();
    }
}
