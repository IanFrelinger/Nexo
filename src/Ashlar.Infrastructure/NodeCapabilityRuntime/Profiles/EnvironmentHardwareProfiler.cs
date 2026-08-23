using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime.Profiles;

/// <summary>
/// Lightweight cross-platform hardware profiler based on environment and runtime data.
/// </summary>
public sealed class EnvironmentHardwareProfiler : IHardwareProfiler
{
    /// <summary>Capture asynchronously.</summary>
    public Task<NodeProfile> CaptureAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var totalRam = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var availableRam = Math.Max(0L, totalRam - GC.GetTotalMemory(forceFullCollection: false));
        var totalVram = TryReadInt64Environment("ASHLAR_TOTAL_VRAM_BYTES");
        var availableVram = TryReadInt64Environment("ASHLAR_AVAILABLE_VRAM_BYTES");

        var isOnBattery = TryReadBoolEnvironment("ASHLAR_ON_BATTERY");
        var isCharging = TryReadBoolEnvironment("ASHLAR_CHARGING");
        var battery = TryReadSingleEnvironment("ASHLAR_BATTERY_PERCENT", 100f);
        var thermal = ParseEnumEnvironment("ASHLAR_THERMAL_STATE", ThermalState.Nominal);
        var appState = ParseEnumEnvironment("ASHLAR_APP_STATE", AppState.Foreground);

        var profile = new NodeProfile
        {
            TotalRAMBytes = totalRam,
            AvailableRAMBytes = availableRam,
            TotalVRAMBytes = totalVram,
            AvailableVRAMBytes = availableVram > 0 ? availableVram : totalVram,
            CPUUtilizationPercent = TryReadSingleEnvironment("ASHLAR_CPU_UTIL_PERCENT", 0f),
            GPUUtilizationPercent = TryReadSingleEnvironment("ASHLAR_GPU_UTIL_PERCENT", 0f),
            IsOnBattery = isOnBattery,
            IsCharging = isCharging,
            BatteryLevelPercent = battery,
            IsUserActive = TryReadBoolEnvironment("ASHLAR_USER_ACTIVE"),
            AppState = appState,
            ThermalState = thermal,
            HasBackgroundTaskPermission = TryReadBoolEnvironment("ASHLAR_BACKGROUND_TASK_PERMISSION"),
            IsBatteryOptimizationEnabled = TryReadBoolEnvironment("ASHLAR_BATTERY_OPTIMIZATION_ENABLED"),
            NetworkLatency = new NetworkLatencyProfile
            {
                IsWifi = TryReadBoolEnvironment("ASHLAR_NETWORK_WIFI", true),
                IsMetered = TryReadBoolEnvironment("ASHLAR_NETWORK_METERED"),
                AverageLatencyMs = (int)TryReadInt64Environment("ASHLAR_NETWORK_LATENCY_MS", 25)
            },
            Storage = new StorageProfile
            {
                AvailableBytes = TryReadInt64Environment("ASHLAR_STORAGE_AVAILABLE_BYTES", 0),
                TotalBytes = TryReadInt64Environment("ASHLAR_STORAGE_TOTAL_BYTES", 0)
            },
            Platform = ResolvePlatform(),
            CapturedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(profile with { Tier = ClassifyTier(profile) });
    }

    private static PlatformType ResolvePlatform()
    {
        if (OperatingSystem.IsWindows()) return PlatformType.Windows;
        if (OperatingSystem.IsMacOS()) return PlatformType.macOS;
        if (OperatingSystem.IsLinux()) return PlatformType.Linux;
        if (OperatingSystem.IsIOS()) return PlatformType.iOS;
        if (OperatingSystem.IsAndroid()) return PlatformType.Android;
        return PlatformType.Unknown;
    }

    /// <summary>Classify tier.</summary>
    public static NodeTier ClassifyTier(NodeProfile profile)
    {
        var ramGiB = profile.TotalRAMBytes / (1024.0 * 1024.0 * 1024.0);
        var vramGiB = profile.TotalVRAMBytes / (1024.0 * 1024.0 * 1024.0);

        if (ramGiB >= 64 || vramGiB >= 24) return NodeTier.Core;
        if (ramGiB >= 24 || vramGiB >= 10) return NodeTier.Standard;
        if (ramGiB >= 10 || vramGiB >= 4) return NodeTier.Micro;
        return NodeTier.Nano;
    }

    private static bool TryReadBoolEnvironment(string key, bool defaultValue = false)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (bool.TryParse(value, out var parsed)) return parsed;
        if (string.Equals(value, "1", StringComparison.Ordinal)) return true;
        if (string.Equals(value, "0", StringComparison.Ordinal)) return false;
        return defaultValue;
    }

    private static long TryReadInt64Environment(string key, long defaultValue = 0)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return long.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static float TryReadSingleEnvironment(string key, float defaultValue = 0f)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return float.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static TEnum ParseEnumEnvironment<TEnum>(string key, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        var value = Environment.GetEnvironmentVariable(key);
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : defaultValue;
    }
}
