using FluentAssertions;
using Microsoft.Extensions.Options;
using GameDirector.Mcp.Forge;
using Xunit;

namespace Nexo.Tests.GameDirector.Api;
/// <summary>Tests for forge map fetch url validator.</summary>
public sealed class ForgeMapFetchUrlValidatorTests
{
    /// <summary>Opts.</summary>
    /// <param name="false">False.</param>
    /// <param name="hosts">Hosts.</param>
    private static ForgeSessionOptions Opts(bool allowEmpty = false, params string[] hosts) =>
        new()
        {
            AllowMapFetchWhenAllowedHostsEmpty = allowEmpty,
            AllowedMapFetchHosts = hosts.ToList()
        };

    [Fact]
    public void Rejects_PrivateIpLiteral()
    {
        ForgeMapFetchUrlValidator.TryValidate("https://10.0.0.1/x", Opts(true), out var err)
            .Should().BeFalse();
        err.Should().Contain("private");
    }

    [Fact]
    public void Rejects_WhenHostNotAllowlisted()
    {
        ForgeMapFetchUrlValidator.TryValidate("https://evil.example/x", Opts(false, "api.mapbox.com"), out var err)
            .Should().BeFalse();
        err.Should().Contain("not in");
    }

    [Fact]
    public void Accepts_AllowlistedHost()
    {
        ForgeMapFetchUrlValidator.TryValidate("https://api.mapbox.com/tiles", Opts(false, "api.mapbox.com"), out var err)
            .Should().BeTrue();
        err.Should().BeNull();
    }

    [Fact]
    public void WildcardSubdomain_Matches()
    {
        ForgeMapFetchUrlValidator.TryValidate("https://a.tile.openstreetmap.org/1", Opts(false, "*.tile.openstreetmap.org"), out var err)
            .Should().BeTrue();
        err.Should().BeNull();
    }
}
