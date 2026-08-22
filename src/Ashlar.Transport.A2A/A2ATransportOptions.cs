namespace Ashlar.Transport.A2A;

/// <summary>
/// Options for the outbound A2A agent transport.
///
/// The transport only ever acts on invocations whose target endpoint carries the
/// <c>a2a+</c> scheme prefix (registered via routing endpoints, e.g.
/// <c>a2a+https://peer.example.com</c>), so the fail-closed default is simply "no A2A endpoints
/// configured anywhere". <see cref="Enabled"/> additionally gates whether the transport is
/// registered at all.
/// </summary>
public sealed class A2ATransportOptions
{
    /// <summary>Default configuration section (<c>Ashlar:A2A:Transport</c>).</summary>
    public const string SectionPath = "Ashlar:A2A:Transport";

    /// <summary>Master switch; defaults to <see langword="false"/> (transport not registered).</summary>
    public bool Enabled { get; set; }

    /// <summary>Per-remote-endpoint settings (API keys), matched by URL prefix.</summary>
    public IList<A2ARemoteEndpointOptions> Endpoints { get; } = new List<A2ARemoteEndpointOptions>();
}

/// <summary>Settings for one remote A2A endpoint (or endpoint prefix).</summary>
public sealed class A2ARemoteEndpointOptions
{
    /// <summary>
    /// http(s) URL prefix these settings apply to (after the <c>a2a+</c> scheme is stripped),
    /// e.g. <c>https://peer.example.com</c>.
    /// </summary>
    public string UrlPrefix { get; set; } = string.Empty;

    /// <summary>Optional header carrying the peer's API key; paired with <see cref="ApiKeyEnvVar"/>.</summary>
    public string? ApiKeyHeader { get; set; }

    /// <summary>Environment variable holding the API key value (never the secret itself).</summary>
    public string? ApiKeyEnvVar { get; set; }
}
