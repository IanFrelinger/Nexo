using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;

/// <summary>
/// Lightweight cross-platform hardware profiler based on environment and runtime data.
/// </summary>
public sealed class EnvironmentHardwareProfiler : IHardwareProfiler
{
    public Task<NodeProfile> CaptureAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var totalRam = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var availableRam = Math.Max(0L, totalRam - GC.GetTotalMemory(forceFullCollection: false));
        var totalVram = TryReadInt64Environment("NEXO_TOTAL_VRAM_BYTES");
        var availableVram = TryReadInt64Environment("NEXO_AVAILABLE_VRAM_BYTES");

        var isOnBattery = TryReadBoolEnvironment("NEXO_ON_BATTERY");
        var isCharging = TryReadBoolEnvironment("NEXO_CHARGING");
        var battery = TryReadSingleEnvironment("NEXO_BATTERY_PERCENT", 100f);
        var thermal = ParseEnumEnvironment("NEXO_THERMAL_STATE", ThermalState.Nominal);
        var appState = ParseEnumEnvironment("NEXO_APP_STATE", AppState.Foreground);

        var profile = new NodeProfile
        {
            TotalRAMBytes = totalRam,
            AvailableRAMBytes = availableRam,
            TotalVRAMBytes = totalVram,
            AvailableVRAMBytes = availableVram > 0 ? availableVram : totalVram,
            CPUUtilizationPercent = TryReadSingleEnvironment("NEXO_CPU_UTIL_PERCENT", 0f),
            GPUUtilizationPercent = TryReadSingleEnvironment("NEXO_GPU_UTIL_PERCENT", 0f),
            IsOnBattery = isOnBattery,
            IsCharging = isCharging,
            BatteryLevelPercent = battery,
            IsUserActive = TryReadBoolEnvironment("NEXO_USER_ACTIVE"),
            AppState = appState,
            ThermalState = thermal,
            HasBackgroundTaskPermission = TryReadBoolEnvironment("NEXO_BACKGROUND_TASK_PERMISSION"),
            IsBatteryOptimizationEnabled = TryReadBoolEnvironment("NEXO_BATTERY_OPTIMIZATION_ENABLED"),
            NetworkLatency = new NetworkLatencyProfile
            {
                IsWifi = TryReadBoolEnvironment("NEXO_NETWORK_WIFI", true),
                IsMetered = TryReadBoolEnvironment("NEXO_NETWORK_METERED"),
                AverageLatencyMs = (int)TryReadInt64Environment("NEXO_NETWORK_LATENCY_MS", 25)
            },
            Storage = new StorageProfile
            {
                AvailableBytes = TryReadInt64Environment("NEXO_STORAGE_AVAILABLE_BYTES", 0),
                TotalBytes = TryReadInt64Environment("NEXO_STORAGE_TOTAL_BYTES", 0)
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
