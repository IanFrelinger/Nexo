namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Snapshot of hardware, power, and platform state for capability routing.
/// </summary>
public sealed record NodeProfile
{
    /// <summary>Free GPU memory available for model loading.</summary>
    public long AvailableVRAMBytes { get; init; }

    /// <summary>Total GPU memory on this node.</summary>
    public long TotalVRAMBytes { get; init; }

    /// <summary>Free system memory available for model loading.</summary>
    public long AvailableRAMBytes { get; init; }

    /// <summary>Total system memory on this node.</summary>
    public long TotalRAMBytes { get; init; }

    /// <summary>Current GPU utilization as a percentage (0–100).</summary>
    public float GPUUtilizationPercent { get; init; }

    /// <summary>Current CPU utilization as a percentage (0–100).</summary>
    public float CPUUtilizationPercent { get; init; }

    /// <summary>Whether the device is running on battery power.</summary>
    public bool IsOnBattery { get; init; }

    /// <summary>Whether the device is currently charging.</summary>
    public bool IsCharging { get; init; }

    /// <summary>Remaining battery level as a percentage (0–100).</summary>
    public float BatteryLevelPercent { get; init; } = 100f;

    /// <summary>Whether the user is actively interacting with the device.</summary>
    public bool IsUserActive { get; init; }

    /// <summary>Current application lifecycle state affecting background work.</summary>
    public AppState AppState { get; init; } = AppState.Foreground;

    /// <summary>Current thermal throttling state of the device.</summary>
    public ThermalState ThermalState { get; init; } = ThermalState.Nominal;

    /// <summary>Whether the app has permission to run background tasks.</summary>
    public bool HasBackgroundTaskPermission { get; init; }

    /// <summary>Whether OS battery optimization is restricting background work.</summary>
    public bool IsBatteryOptimizationEnabled { get; init; }

    /// <summary>Network connectivity and latency characteristics.</summary>
    public NetworkLatencyProfile NetworkLatency { get; init; } = new();

    /// <summary>Available and total on-device storage.</summary>
    public StorageProfile Storage { get; init; } = new();

    /// <summary>Capability tier assigned to this node.</summary>
    public NodeTier Tier { get; init; } = NodeTier.Nano;

    /// <summary>Host platform of the runtime.</summary>
    public PlatformType Platform { get; init; } = PlatformType.Unknown;

    /// <summary>UTC timestamp when this profile was captured.</summary>
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
}
