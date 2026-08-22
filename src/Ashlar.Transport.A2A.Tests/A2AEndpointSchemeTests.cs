using FluentAssertions;
using Xunit;

namespace Ashlar.Transport.A2A.Tests;

public sealed class A2AEndpointSchemeTests
{
    [Theory]
    [InlineData("a2a+https://peer.example.com", true)]
    [InlineData("A2A+http://localhost:8080/api/a2a/agent", true)]
    [InlineData("https://peer.example.com", false)]
    [InlineData("grpc://peer", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsA2A_detects_the_scheme_prefix(string? endpoint, bool expected)
    {
        A2AEndpointScheme.IsA2A(endpoint).Should().Be(expected);
    }

    [Fact]
    public void TryGetHttpBaseUrl_strips_the_prefix()
    {
        A2AEndpointScheme.TryGetHttpBaseUrl("a2a+https://peer.example.com/api/a2a/echo", out var url).Should().BeTrue();

        url.Should().Be(new Uri("https://peer.example.com/api/a2a/echo"));
    }

    [Theory]
    [InlineData("a2a+ftp://peer.example.com")]
    [InlineData("a2a+not-a-url")]
    [InlineData("https://peer.example.com")]
    public void TryGetHttpBaseUrl_rejects_non_http_or_unprefixed_endpoints(string endpoint)
    {
        A2AEndpointScheme.TryGetHttpBaseUrl(endpoint, out _).Should().BeFalse();
    }
}
