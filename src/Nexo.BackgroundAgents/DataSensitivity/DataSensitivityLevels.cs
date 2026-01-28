namespace Nexo.BackgroundAgents.DataSensitivity;

/// <summary>
/// Primitive sensitivity levels provided by the framework.
/// 
/// Provides five standard sensitivity levels:
/// - Public: No restrictions
/// - Internal: Internal use only
/// - Confidential: Restricted access, blocks external LLMs
/// - Secret: Highly restricted, blocks external LLMs and web search
/// - TopSecret: Maximum restrictions, requires local-only processing
/// 
/// Users can define custom sensitivity levels via configuration.
/// </summary>
public static class DataSensitivityLevels
{
    /// <summary>
    /// Public data, no restrictions.
    /// </summary>
    public static IDataSensitivityLevel Public { get; } = new PrimitiveSensitivityLevel(
        "Public",
        "Public",
        0,
        AllowsExternalLLM: true,
        AllowsWebSearch: true,
        RequiresLocalOnly: false,
        AllowsNetworkExports: true,
        "Public data, no restrictions");

    /// <summary>
    /// Internal use only.
    /// </summary>
    public static IDataSensitivityLevel Internal { get; } = new PrimitiveSensitivityLevel(
        "Internal",
        "Internal",
        1,
        AllowsExternalLLM: true,
        AllowsWebSearch: true,
        RequiresLocalOnly: false,
        AllowsNetworkExports: true,
        "Internal use only");

    /// <summary>
    /// Confidential, restricted access.
    /// </summary>
    public static IDataSensitivityLevel Confidential { get; } = new PrimitiveSensitivityLevel(
        "Confidential",
        "Confidential",
        2,
        AllowsExternalLLM: false,
        AllowsWebSearch: true,
        RequiresLocalOnly: false,
        AllowsNetworkExports: false,
        "Confidential, restricted access");

    /// <summary>
    /// Secret, highly restricted.
    /// </summary>
    public static IDataSensitivityLevel Secret { get; } = new PrimitiveSensitivityLevel(
        "Secret",
        "Secret",
        3,
        AllowsExternalLLM: false,
        AllowsWebSearch: false,
        RequiresLocalOnly: false,
        AllowsNetworkExports: false,
        "Secret, highly restricted");

    /// <summary>
    /// Top secret, maximum restrictions.
    /// </summary>
    public static IDataSensitivityLevel TopSecret { get; } = new PrimitiveSensitivityLevel(
        "TopSecret",
        "Top Secret",
        4,
        AllowsExternalLLM: false,
        AllowsWebSearch: false,
        RequiresLocalOnly: true,
        AllowsNetworkExports: false,
        "Top secret, maximum restrictions");

    /// <summary>
    /// Get sensitivity level by name (case-insensitive).
    /// </summary>
    /// <param name="name">The sensitivity level name.</param>
    /// <returns>The sensitivity level, or null if not found.</returns>
    public static IDataSensitivityLevel? FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return name.ToLowerInvariant() switch
        {
            "public" => Public,
            "internal" => Internal,
            "confidential" => Confidential,
            "secret" => Secret,
            "topsecret" or "top-secret" => TopSecret,
            _ => null
        };
    }

    /// <summary>
    /// All primitive sensitivity levels in order (by SensitivityValue).
    /// </summary>
    public static IReadOnlyList<IDataSensitivityLevel> All => new[]
    {
        Public, Internal, Confidential, Secret, TopSecret
    };
}
