namespace Nexo.GameDomain.Assets;

/// <summary>
/// Comprehensive audio asset descriptor for Unity. Covers sound effects,
/// music tracks, ambient soundscapes, and UI audio. Generates AudioClip
/// configuration, AudioMixer groups, and spatial audio settings.
/// </summary>
public sealed record AudioDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "sfx"; // sfx, music, ambient, ui, voice, foley
    public string SubCategory { get; init; } = string.Empty; // weapon_fire, footstep, explosion, pickup, etc.

    // Playback
    public double Volume { get; init; } = 1.0;
    public double Pitch { get; init; } = 1.0;
    public double PitchVariance { get; init; } = 0.0;
    public bool Loop { get; init; }
    public double FadeInSeconds { get; init; }
    public double FadeOutSeconds { get; init; }
    public int Priority { get; init; } = 128;

    // Spatial
    public double SpatialBlend { get; init; } = 1.0;
    public double MinDistance { get; init; } = 1.0;
    public double MaxDistance { get; init; } = 50.0;
    public string RolloffMode { get; init; } = "logarithmic";
    public double DopplerLevel { get; init; } = 1.0;
    public double Spread { get; init; } = 0.0;

    // Mixer
    public string MixerGroup { get; init; } = "Master";
    public double DuckingVolume { get; init; } = 1.0;
    public bool BypassEffects { get; init; }
    public bool BypassReverb { get; init; }

    // Variations (for randomized playback)
    public IReadOnlyList<AudioVariation> Variations { get; init; } = Array.Empty<AudioVariation>();

    // Tags for querying and categorization
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record AudioVariation
{
    public string VariantId { get; init; } = string.Empty;
    public double PitchOffset { get; init; }
    public double VolumeOffset { get; init; }
    public string? ClipSuffix { get; init; }
}
