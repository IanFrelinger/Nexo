namespace Ashlar.Transport.A2A;

/// <summary>
/// The <c>a2a+</c> endpoint scheme convention: routing endpoints of the form
/// <c>a2a+https://host[:port][/path]</c> select the A2A transport; stripping the prefix yields
/// the peer's real http(s) base URL. Chosen over adding a protocol field to
/// <c>EndpointDescriptor</c> because that record is public API on the packable, netstandard2.0
/// kernel spine — a scheme prefix in the endpoint string is invisible to every existing
/// consumer and routing policy.
/// </summary>
public static class A2AEndpointScheme
{
    /// <summary>The endpoint prefix registered with the scheme-dispatching transport.</summary>
    public const string Prefix = "a2a+";

    /// <summary>Whether the endpoint targets the A2A transport.</summary>
    public static bool IsA2A(string? endpoint)
        => endpoint is not null && endpoint.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Strips the scheme prefix, returning the peer's http(s) base URL.
    /// Returns false when the endpoint is not an <c>a2a+</c> endpoint or the remainder is not an
    /// absolute http(s) URL.
    /// </summary>
    public static bool TryGetHttpBaseUrl(string? endpoint, out Uri baseUrl)
    {
        baseUrl = null!;
        if (!IsA2A(endpoint))
        {
            return false;
        }

        var stripped = endpoint![Prefix.Length..];
        return Uri.TryCreate(stripped, UriKind.Absolute, out baseUrl!) &&
               (baseUrl.Scheme == Uri.UriSchemeHttp || baseUrl.Scheme == Uri.UriSchemeHttps);
    }
}
