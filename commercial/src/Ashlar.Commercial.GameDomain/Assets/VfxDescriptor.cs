namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Visual effect descriptor (particles, ribbons, GPU VFX). Covers configurations for
/// muzzle flash, impacts, explosions, trails, ambient effects.
/// </summary>
public sealed record VfxDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "impact"; // muzzle_flash, impact, explosion, trail, ambient, shield, pickup, death

    /// <summary>Duration value.</summary>
    public double Duration { get; init; } = 1.0;
    /// <summary>Loop value.</summary>
    public bool Loop { get; init; }
    /// <summary>Play on awake value.</summary>
    public bool PlayOnAwake { get; init; } = true;

    /// <summary>Modules value.</summary>
    public IReadOnlyList<ParticleModule> Modules { get; init; } = Array.Empty<ParticleModule>();
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
