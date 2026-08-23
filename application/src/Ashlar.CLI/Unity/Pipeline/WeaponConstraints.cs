namespace Ashlar.CLI.Unity.Pipeline;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Combat tuning bounds for generated weapon systems.</summary>
public sealed record WeaponConstraints
{
    /// <summary>Allowed damage range as <c>[min, max]</c> inclusive.</summary>
    public double[]? DamageRange { get; init; }

    /// <summary>Allowed fire-rate range as <c>[min, max]</c> shots per second.</summary>
    public double[]? FireRateRange { get; init; }

    /// <summary>Maximum magazine capacity for generated weapons.</summary>
    public int MaxMagazineSize { get; init; }

    /// <summary>When true, weapons must implement a reload mechanic.</summary>
    public bool RequireReloadMechanic { get; init; }
}
