namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// Provider-specific configuration for model selection.
/// </summary>
public class ProviderConfig
{
    /// <summary>Provider-specific model id.</summary>
    public string Model { get; init; } = default!;

    /// <summary>Optional provider endpoint override.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Maps a provider to a model and optional endpoint.</summary>
    /// <param name="model">Provider-specific model id.</param>
    /// <param name="endpoint">Optional endpoint override.</param>
    public ProviderConfig(string model, string? endpoint = null)
    {
        Model = model;
        Endpoint = endpoint;
    }
}
