namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Shared validation helpers for background agent runner adapters.
/// </summary>
internal static class BackgroundAgentAdapterValidation
{
    /// <summary>
    /// Validates that the path is non-empty and refers to an existing directory.
    /// </summary>
    /// <param name="path">Path to validate.</param>
    /// <param name="paramName">Parameter name for error messages (e.g. "Path", "RepoRoot").</param>
    /// <param name="errorMessage">When false, the reason validation failed.</param>
    /// <returns>True if path is valid and directory exists; otherwise false.</returns>
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
