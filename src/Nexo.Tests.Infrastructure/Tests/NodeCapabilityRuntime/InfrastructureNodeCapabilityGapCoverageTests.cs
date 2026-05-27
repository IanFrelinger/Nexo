using System.Reflection;
using FluentAssertions;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Infrastructure.NodeCapabilityRuntime.Policies;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.NodeCapabilityRuntime;

public class InfrastructureNodeCapabilityGapCoverageTests
{
    [Fact]
    public void AndroidPolicy_covers_inference_load_model_pull_and_maintenance_rules()
    {
        var policy = new AndroidPolicy
        {
            AllowCellularPull = true,
            MinBatteryForRemoteWork = 25f,
            UseVulkanCompute = true,
            UseNNAPI = true,
            DozeAware = true,
        };

        policy.Platform.Should().Be(PlatformType.Android);
        policy.MaxConcurrentInferenceRequests.Should().Be(1);
        policy.HotModelTTL.Should().Be(TimeSpan.FromMinutes(15));

        var healthy = new NodeProfile
        {
            ThermalState = ThermalState.Fair,
            AppState = AppState.Foreground,
            AvailableRAMBytes = 4L * 1024 * 1024 * 1024,
            BatteryLevelPercent = 80f,
            NetworkLatency = new NetworkLatencyProfile { IsWifi = false },
            IsCharging = true,
            IsBatteryOptimizationEnabled = false,
        };

        policy.CanRunInferenceNow(healthy).Should().BeTrue();
        policy.CanPullModel(healthy).Should().BeTrue();
        policy.CanAdvertiseRemoteWork(healthy).Should().BeFalse();

        var lowRam = healthy with { AvailableRAMBytes = 256L * 1024 * 1024 };
        policy.ShouldUnloadAfterInference(lowRam).Should().BeTrue();
        policy.CanRunIdleMaintenance(healthy with { NetworkLatency = new NetworkLatencyProfile { IsWifi = true } })
            .Should().BeTrue();

        var hugeModel = new ModelDescriptor
        {
            WeightSizeBytes = policy.MaxSingleModelBytes + 1,
            MinRAMRequiredBytes = 1024,
        };
        policy.CanLoadModel(hugeModel, healthy).Should().BeFalse();
    }

    [Fact]
    public void WindowsPolicy_covers_inference_remote_work_and_vram_unload()
    {
        var policy = new WindowsPolicy
        {
            AllowCUDA = true,
            AllowDirectML = true,
            EnforceDefenderExclusions = true,
        };

        policy.Platform.Should().Be(PlatformType.Windows);
        policy.CanPullModel(new NodeProfile()).Should().BeTrue();
        policy.MaxConcurrentInferenceRequests.Should().Be(4);

        var profile = new NodeProfile
        {
            GPUUtilizationPercent = 50f,
            CPUUtilizationPercent = 40f,
            AvailableVRAMBytes = 2L * 1024 * 1024 * 1024,
            AvailableRAMBytes = 16L * 1024 * 1024 * 1024,
            IsUserActive = false,
        };

        policy.CanRunInferenceNow(profile).Should().BeTrue();
        policy.CanAdvertiseRemoteWork(profile).Should().BeTrue();
        policy.CanRunIdleMaintenance(profile).Should().BeTrue();

        var model = new ModelDescriptor
        {
            MinVRAMRequiredBytes = 1L * 1024 * 1024 * 1024,
            MinRAMRequiredBytes = 4L * 1024 * 1024 * 1024,
        };
        policy.CanLoadModel(model, profile).Should().BeTrue();

        policy.ShouldUnloadAfterInference(profile with { AvailableVRAMBytes = 256L * 1024 * 1024 }).Should().BeTrue();
        policy.CanAdvertiseRemoteWork(profile with { GPUUtilizationPercent = 90f }).Should().BeFalse();
    }

    [Fact]
    public void MacOsPolicy_covers_battery_remote_work_and_idle_maintenance()
    {
        var policy = new MacOsPolicy();
        policy.Platform.Should().Be(PlatformType.macOS);
        policy.MaxConcurrentInferenceRequests.Should().Be(2);

        var profile = new NodeProfile
        {
            ThermalState = ThermalState.Nominal,
            GPUUtilizationPercent = 50f,
            AvailableRAMBytes = 16L * 1024 * 1024 * 1024,
            IsOnBattery = true,
            IsCharging = false,
            IsUserActive = false,
        };

        policy.CanRunInferenceNow(profile).Should().BeTrue();
        policy.CanPullModel(profile with { IsOnBattery = false }).Should().BeTrue();
        policy.CanPullModel(profile with { IsCharging = true }).Should().BeTrue();
        policy.CanPullModel(profile with { BatteryLevelPercent = 60f }).Should().BeTrue();
        policy.CanAdvertiseRemoteWork(profile with { IsCharging = true, CPUUtilizationPercent = 40f }).Should().BeTrue();
        policy.ShouldUnloadAfterInference(profile).Should().BeTrue();
        policy.CanRunIdleMaintenance(profile).Should().BeTrue();

        policy.CanRunInferenceNow(profile with { ThermalState = ThermalState.Serious }).Should().BeFalse();
        policy.CanRunInferenceNow(profile with { GPUUtilizationPercent = 95f }).Should().BeFalse();
        policy.CanLoadModel(
            new ModelDescriptor { MinRAMRequiredBytes = profile.AvailableRAMBytes + 1 },
            profile).Should().BeFalse();
    }

    [Fact]
    public void LinuxPolicy_covers_gpu_remote_work_and_init_properties()
    {
        var policy = new LinuxPolicy
        {
            AllowCUDA = false,
            AllowROCm = true,
            AllowVulkan = false,
            UseHugePages = true,
            OllamaNumGPU = 2,
            IsContainerized = true,
            IsHeadless = true,
        };

        policy.AllowCUDA.Should().BeFalse();
        policy.IsContainerized.Should().BeTrue();

        var profile = new NodeProfile
        {
            GPUUtilizationPercent = 40f,
            AvailableVRAMBytes = 8L * 1024 * 1024 * 1024,
            AvailableRAMBytes = 16L * 1024 * 1024 * 1024,
        };

        policy.CanRunInferenceNow(profile).Should().BeTrue();
        policy.CanAdvertiseRemoteWork(profile).Should().BeTrue();
        policy.CanRunIdleMaintenance(profile).Should().BeTrue();
        policy.ShouldUnloadAfterInference(profile).Should().BeFalse();

        policy.CanRunInferenceNow(profile with { GPUUtilizationPercent = 99f }).Should().BeFalse();
        policy.CanAdvertiseRemoteWork(profile with { GPUUtilizationPercent = 60f }).Should().BeFalse();
        policy.CanLoadModel(
            new ModelDescriptor
            {
                MinVRAMRequiredBytes = profile.AvailableVRAMBytes + 1,
                MinRAMRequiredBytes = 1024,
            },
            profile).Should().BeFalse();
    }

    [Fact]
    public void iOSPolicy_covers_background_wifi_and_battery_rules()
    {
        var policy = new iOSPolicy
        {
            MinBatteryForRemoteWork = 30f,
            UseNeuralEngine = false,
            UseMetal = false,
            AllowLowPowerModeInference = true,
        };

        policy.MinBatteryForRemoteWork.Should().Be(30f);
        policy.AllowLowPowerModeInference.Should().BeTrue();

        var profile = new NodeProfile
        {
            AppState = AppState.Foreground,
            AvailableRAMBytes = 4L * 1024 * 1024 * 1024,
            NetworkLatency = new NetworkLatencyProfile { IsWifi = true },
            IsOnBattery = false,
            IsCharging = true,
            BatteryLevelPercent = 80f,
            ThermalState = ThermalState.Nominal,
            HasBackgroundTaskPermission = true,
        };

        policy.CanRunInferenceNow(profile).Should().BeTrue();
        policy.CanPullModel(profile).Should().BeTrue();
        policy.CanAdvertiseRemoteWork(profile with
        {
            AppState = AppState.Background,
            BatteryLevelPercent = 10f,
        }).Should().BeFalse();
        policy.CanRunIdleMaintenance(profile with { AppState = AppState.Background }).Should().BeTrue();

        policy.CanRunInferenceNow(profile with
        {
            AppState = AppState.Background,
            HasBackgroundTaskPermission = false,
        }).Should().BeFalse();

        policy.CanLoadModel(
            new ModelDescriptor
            {
                WeightSizeBytes = policy.MaxSingleModelBytes + 1,
                MinRAMRequiredBytes = 1024,
            },
            profile).Should().BeFalse();
    }

    [Fact]
    public void SimpleObservable_publishes_to_subscribers_and_unsubscribes()
    {
        var observableType = typeof(Nexo.Infrastructure.NodeCapabilityRuntime.NodeCapabilityRuntime).Assembly
            .GetType("Nexo.Infrastructure.NodeCapabilityRuntime.SimpleObservable`1")!
            .MakeGenericType(typeof(int));
        var observable = Activator.CreateInstance(observableType)!;
        var publish = observableType.GetMethod("Publish")!;

        var seen = new List<int>();
        var observer = new TestObserver<int>(seen);
        var subscribe = (IDisposable)observableType.GetMethod("Subscribe")!.Invoke(observable, new object[] { observer })!;

        publish.Invoke(observable, new object[] { 1 });
        publish.Invoke(observable, new object[] { 2 });
        seen.Should().Equal(1, 2);

        subscribe.Dispose();
        publish.Invoke(observable, new object[] { 3 });
        seen.Should().Equal(1, 2);

        var act = () => observableType.GetMethod("Subscribe")!.Invoke(observable, new object[] { null! });
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentNullException>();
    }

    private sealed class TestObserver<T>(List<T> sink) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) => throw error;
        public void OnNext(T value) => sink.Add(value);
    }
}
