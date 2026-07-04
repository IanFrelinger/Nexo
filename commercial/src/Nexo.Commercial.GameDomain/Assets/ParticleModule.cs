namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Particle module record.</summary>
public sealed record ParticleModule
{
    /// <summary>Module name value.</summary>
    public string ModuleName { get; init; } = string.Empty; // emission, shape, velocity, color, size, noise, collision, renderer
    /// <summary>Properties value.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}
