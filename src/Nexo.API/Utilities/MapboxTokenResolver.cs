namespace Nexo.API.Utilities;

/// <summary>
/// Utility for resolving Mapbox access tokens from environment variables or provided values.
/// </summary>
public static class MapboxTokenResolver
{
    /// <summary>
    /// Resolves a Mapbox access token from the provided value or environment variable.
    /// </summary>
    /// <param name="token">Token provided by user, or null to use environment variable</param>
    /// <returns>Resolved token</returns>
    /// <exception cref="InvalidOperationException">Thrown if token cannot be resolved</exception>
    public static string Resolve(string? token)
    {
        token = string.IsNullOrWhiteSpace(token) ? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN") : token;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Mapbox token is required (set MAPBOX_ACCESS_TOKEN or pass --mapbox-token).");
        return token.Trim();
    }
}
