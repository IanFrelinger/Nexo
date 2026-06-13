namespace Nexo.Runtime.Barriers.Identity.Resolvers;

internal sealed class ApiKeyResolverOptions
{
    /// <summary>
    /// Header name to read API key from. Default: x-nexo-api-key.
    /// </summary>
    public string HeaderName { get; init; } = "x-nexo-api-key";

    /// <summary>
    /// Maps SHA-256 API key hashes (hex) to barrier levels.
    /// Never store plaintext keys in this mapping.
    /// </summary>
    public IDictionary<string, string> KeyMapping { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
