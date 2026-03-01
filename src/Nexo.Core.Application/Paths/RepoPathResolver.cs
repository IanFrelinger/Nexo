namespace Nexo.Core.Application.Paths;

/// <summary>
/// Shared utilities for resolving repository and observation paths.
/// </summary>
public static class RepoPathResolver
{
    /// <summary>
    /// Walks up from the given directory until Nexo.sln is found.
    /// </summary>
    /// <param name="startDir">Starting directory. Defaults to <see cref="Environment.CurrentDirectory"/>.</param>
    /// <returns>Repository root directory path, or current directory if not found.</returns>
    public static string FindRepoRoot(string? startDir = null)
    {
        var dir = new DirectoryInfo(startDir ?? Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Returns the Block 1 Observation path when running in the Nexo repo.
    /// Prefers src/Nexo.Infrastructure/Observation, then src/Nexo.BackgroundAgents/Observation.
    /// </summary>
    /// <param name="repoRoot">Repository root. If null, uses <see cref="FindRepoRoot"/>.</param>
    /// <returns>Path to Observation folder, or current directory if not in Nexo repo.</returns>
    public static string FindBlock1ObservationPath(string? repoRoot = null)
    {
        var root = repoRoot ?? FindRepoRoot();
        var sln = Path.Combine(root, "Nexo.sln");
        if (!File.Exists(sln))
            return Directory.GetCurrentDirectory();

        var infraObs = Path.Combine(root, "src", "Nexo.Infrastructure", "Observation");
        var bgObs = Path.Combine(root, "src", "Nexo.BackgroundAgents", "Observation");
        if (Directory.Exists(infraObs))
            return infraObs;
        if (Directory.Exists(bgObs))
            return bgObs;
        return Directory.GetCurrentDirectory();
    }
}
