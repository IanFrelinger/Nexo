namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Audio asset descriptor: sound effects, music, ambient soundscapes, and UI audio,
/// including playback, bus routing, and spatial settings for the host audio engine.
/// </summary>
public sealed record AudioDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "sfx"; // sfx, music, ambient, ui, voice, foley
    /// <summary>Sub category value.</summary>
    public string SubCategory { get; init; } = string.Empty; // weapon_fire, footstep, explosion, pickup, etc.

    // Playback
    /// <summary>Volume value.</summary>
    public double Volume { get; init; } = 1.0;
    /// <summary>Pitch value.</summary>
    public double Pitch { get; init; } = 1.0;
    /// <summary>Pitch variance value.</summary>
    public double PitchVariance { get; init; } = 0.0;
    /// <summary>Loop value.</summary>
    public bool Loop { get; init; }
    /// <summary>Fade in seconds value.</summary>
    public double FadeInSeconds { get; init; }
    /// <summary>Fade out seconds value.</summary>
    public double FadeOutSeconds { get; init; }
    /// <summary>Priority value.</summary>
    public int Priority { get; init; } = 128;

    // Spatial
    /// <summary>Spatial blend value.</summary>
    public double SpatialBlend { get; init; } = 1.0;
    /// <summary>Min distance value.</summary>
    public double MinDistance { get; init; } = 1.0;
    /// <summary>Max distance value.</summary>
    public double MaxDistance { get; init; } = 50.0;
    /// <summary>Rolloff mode value.</summary>
    public string RolloffMode { get; init; } = "logarithmic";
    /// <summary>Doppler level value.</summary>
    public double DopplerLevel { get; init; } = 1.0;
    /// <summary>Spread value.</summary>
    public double Spread { get; init; } = 0.0;

    // Mixer
    /// <summary>Mixer group value.</summary>
    public string MixerGroup { get; init; } = "Master";
    /// <summary>Ducking volume value.</summary>
    public double DuckingVolume { get; init; } = 1.0;
    /// <summary>Bypass effects value.</summary>
    public bool BypassEffects { get; init; }
    /// <summary>Bypass reverb value.</summary>
    public bool BypassReverb { get; init; }

    // Variations (for randomized playback)
    /// <summary>Variations value.</summary>
    public IReadOnlyList<AudioVariation> Variations { get; init; } = Array.Empty<AudioVariation>();

    // Tags for querying and categorization
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
