using FluentAssertions;
using Ashlar.Infrastructure.Testing;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Testing;

/// <summary>Tests for platform capability detector gap coverage.</summary>
public sealed class PlatformCapabilityDetectorGapCoverageTests
{
    [Theory]
    [InlineData("ubuntu", true)]
    [InlineData("Alpine-Linux", true)]
    [InlineData("debian", true)]
    [InlineData("android", true)]
    [InlineData("windows-container", true)]
    [InlineData("unity", true)]
    [InlineData("ios-simulator", false)]
    [InlineData("unknown-platform", true)]
    public void CanUseDocker_returns_expected_value(string platformName, bool expected)
    {
        PlatformCapabilityDetector.CanUseDocker(platformName).Should().Be(expected);
    }

    [Theory]
    [InlineData("ios-device", true)]
    [InlineData("macos", false)]
    public void RequiresNativeExecution_identifies_ios_only(string platformName, bool expected)
    {
        PlatformCapabilityDetector.RequiresNativeExecution(platformName).Should().Be(expected);
    }

    [Theory]
    [InlineData("ios", ExecutionMode.Native)]
    [InlineData("ubuntu", ExecutionMode.Docker)]
    public void GetExecutionMode_maps_platform_to_mode(string platformName, ExecutionMode expected)
    {
        PlatformCapabilityDetector.GetExecutionMode(platformName).Should().Be(expected);
    }

    [Fact]
    public void IsPlatformSupported_checks_host_os_for_ios_and_windows()
    {
        PlatformCapabilityDetector.IsPlatformSupported("linux").Should().BeTrue();
        PlatformCapabilityDetector.IsPlatformSupported("ios").Should().Be(
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.OSX));
        PlatformCapabilityDetector.IsPlatformSupported("windows").Should().Be(
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows));
    }
}
