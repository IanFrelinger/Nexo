namespace Ashlar.BackgroundAgents.HostRunners;

/// <summary>
/// Shared validation helpers for background agent runner adapters.
/// </summary>
internal static class BackgroundAgentAdapterValidation
{
    /// <summary>
    /// Validates that the path is non-empty and refers to an existing directory.
    /// </summary>
    public static bool TryResolveDirectory(string? path, string paramName, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = $"{paramName} is empty.";
            return false;
        }

        var dir = new DirectoryInfo(path);
        if (!dir.Exists)
        {
            errorMessage = $"{paramName} does not exist: {path}";
            return false;
        }

        return true;
    }
}
