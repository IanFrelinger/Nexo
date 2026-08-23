namespace Ashlar.Client;

/// <summary>
/// Options for configuring the Ashlar API client.
/// </summary>
public sealed class AshlarClientOptions
{
    /// <summary>
    /// Base URL of the Ashlar API (e.g. https://your-server:5000 or http://192.168.1.10:5000).
    /// </summary>
    public required string BaseUrl { get; set; }

    /// <summary>
    /// Optional API key for authentication (e.g. X-Api-Key header).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Timeout for HTTP requests. Default: 60 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
}
