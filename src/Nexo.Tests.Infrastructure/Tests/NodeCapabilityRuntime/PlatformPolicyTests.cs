using FluentAssertions;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public sealed class PlatformPolicyTests
{
    [Fact]
    public void LinuxPolicy_CanAdvertiseRemoteWork_WhenGpuLow()
    {
        var policy = new LinuxPolicy();
        var profile = new NodeProfile { GPUUtilizationPercent = 20f };

        policy.CanAdvertiseRemoteWork(profile).Should().BeTrue();
    }

    [Fact]
    public void LinuxPolicy_CanAdvertiseRemoteWork_WhenGpuHigh_ShouldBeFalse()
    {
        var policy = new LinuxPolicy();
        var profile = new NodeProfile { GPUUtilizationPercent = 75f };

        policy.CanAdvertiseRemoteWork(profile).Should().BeFalse();
    }

    [Fact]
    public void WindowsPolicy_CanLoadModel_RequiresVramAndRam()
    {
        var policy = new WindowsPolicy();
        var profile = new NodeProfile
        {
            AvailableVRAMBytes = 3L * 1024 * 1024 * 1024,
            AvailableRAMBytes = 8L * 1024 * 1024 * 1024
        };
        var model = new ModelDescriptor
        {
            MinVRAMRequiredBytes = 4L * 1024 * 1024 * 1024,
            MinRAMRequiredBytes = 6L * 1024 * 1024 * 1024
        };

        policy.CanLoadModel(model, profile).Should().BeFalse();
    }

    [Fact]
    public void AndroidPolicy_CanPullModel_RespectsCellularFlag()
    {
        var policy = new AndroidPolicy { AllowCellularPull = false };
        var profile = new NodeProfile
        {
            BatteryLevelPercent = 80f,
            NetworkLatency = new NetworkLatencyProfile { IsWifi = false }
        };

        policy.CanPullModel(profile).Should().BeFalse();
    }

    [Fact]
    public void MacPolicy_ShouldUnloadAfterInference_OnBattery()
    {
        var policy = new MacOsPolicy();
        var profile = new NodeProfile
        {
            IsOnBattery = true,
            IsCharging = false
        };

        policy.ShouldUnloadAfterInference(profile).Should().BeTrue();
    }

    [Fact]
    public void IOSPolicy_CanAdvertiseRemoteWork_RequiresBackgroundChargingWifi()
    {
        var policy = new iOSPolicy();
        var profile = new NodeProfile
        {
            IsCharging = true,
            BatteryLevelPercent = 80f,
            AppState = AppState.Background,
            ThermalState = ThermalState.Fair,
            NetworkLatency = new NetworkLatencyProfile { IsWifi = true }
        };

        policy.CanAdvertiseRemoteWork(profile).Should().BeTrue();
    }
}
