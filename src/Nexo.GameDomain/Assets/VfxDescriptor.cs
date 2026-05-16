namespace Nexo.GameDomain.Assets;

/// <summary>
/// Visual effect descriptor (particles, ribbons, GPU VFX). Covers configurations for
/// muzzle flash, impacts, explosions, trails, ambient effects.
/// </summary>
public sealed record VfxDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "impact"; // muzzle_flash, impact, explosion, trail, ambient, shield, pickup, death

    public double Duration { get; init; } = 1.0;
    public bool Loop { get; init; }
    public bool PlayOnAwake { get; init; } = true;

    public IReadOnlyList<ParticleModule> Modules { get; init; } = Array.Empty<ParticleModule>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record ParticleModule
{
    public string ModuleName { get; init; } = string.Empty; // emission, shape, velocity, color, size, noise, collision, renderer
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}
