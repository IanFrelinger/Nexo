using System.Text;
using FluentAssertions;
using Ashlar.Certification.Physical;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Canonical asset-hash tests: known SHA-256 vector, overload agreement, and the
/// guard clauses that refuse to hash nothing (an empty hash input would otherwise
/// mint a certifiable digest of no content).
/// </summary>
[Trait("Category", "Certification")]
public sealed class AssetContentHasherTests
{
    private const string AbcSha256 =
        "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    [Fact]
    public void A1_StringOverload_MatchesKnownSha256VectorLowercase()
    {
        var digest = AssetContentHasher.ComputeSha256Hex("abc");

        digest.Should().Be(AbcSha256);
    }

    [Fact]
    public void A2_SpanOverload_AgreesWithStringOverloadOnUtf8Bytes()
    {
        var fromBytes = AssetContentHasher.ComputeSha256Hex(Encoding.UTF8.GetBytes("abc"));

        fromBytes.Should().Be(AbcSha256);
    }

    [Fact]
    public void R1_EmptyAssetBytes_Throws()
    {
        var act = () => AssetContentHasher.ComputeSha256Hex(ReadOnlySpan<byte>.Empty);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("assetBytes");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void R2_NullOrEmptyCanonicalSource_Throws(string? canonicalSource)
    {
        var act = () => AssetContentHasher.ComputeSha256Hex(canonicalSource!);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("canonicalSource");
    }
}
