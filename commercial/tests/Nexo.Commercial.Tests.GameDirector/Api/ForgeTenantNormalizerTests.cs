using FluentAssertions;
using GameDirector.Mcp.Forge;
using Xunit;

namespace Nexo.Tests.GameDirector.Api;
/// <summary>Tests for forge tenant normalizer.</summary>
public sealed class ForgeTenantNormalizerTests
{
    [Theory]
    [InlineData(null, ForgeTenantNormalizer.DefaultTenantId)]
    [InlineData("", ForgeTenantNormalizer.DefaultTenantId)]
    [InlineData("  acme-corp  ", "acme-corp")]
    [InlineData("team@#$beta", "team---beta")]
    public void Normalize_ReturnsSafeId(string? raw, string expected)
    {
        ForgeTenantNormalizer.Normalize(raw).Should().Be(expected);
    }
}
