using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Ashlar.API.Endpoints;
using Ashlar.API.Security;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.API;

/// <summary>Tests for security advisory endpoint.</summary>
public sealed class SecurityAdvisoryEndpointTests
{
    [Theory]
    [InlineData("Localhost", "Localhost", "this machine only")]
    [InlineData("Lan", "Lan", "local network")]
    [InlineData("Tailnet", "Tailnet", "Tailscale")]
    [InlineData("Public", "Public", "Internet")]
    public void GetSecurityAdvisory_MapsConfiguredExposureProfile(
        string configuredProfile,
        string expectedProfile,
        string expectedSummaryFragment)
    {
        var response = InvokeGetSecurityAdvisory(new AshlarSecurityOptions
        {
            ExposureProfile = configuredProfile
        });

        response.ExposureProfile.Should().Be(expectedProfile);
        response.Summary.Should().Contain(expectedSummaryFragment);
        response.Hints.Should().NotBeEmpty();
    }

    [Fact]
    public void GetSecurityAdvisory_WhenProfileIsInvalid_FallsBackToLocalhost()
    {
        var response = InvokeGetSecurityAdvisory(new AshlarSecurityOptions
        {
            ExposureProfile = "not-a-real-profile"
        });

        response.ExposureProfile.Should().Be("Localhost");
        response.Summary.Should().Contain("this machine only");
    }

    private static SecurityAdvisoryResponse InvokeGetSecurityAdvisory(AshlarSecurityOptions options)
    {
        var method = typeof(AshlarEndpoints).GetMethod(
            "GetSecurityAdvisory",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull("security advisory endpoint handler should exist");

        var result = (IResult)method!.Invoke(
            null,
            [Options.Create(options)])!;

        var valueProperty = result.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
        valueProperty.Should().NotBeNull();

        var value = valueProperty!.GetValue(result);
        value.Should().BeOfType<SecurityAdvisoryResponse>();
        return (SecurityAdvisoryResponse)value!;
    }
}
