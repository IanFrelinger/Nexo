using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.Observations;

namespace Ashlar.BackgroundAgents.RuntimeStudio;

/// <summary>
/// Resolves runtime-studio on-disk paths from <c>ASHLAR_*</c> overrides and an application base directory
/// (API content root or repo root for CLI).
/// </summary>
public static class RuntimeStudioPathResolver
{
    public sealed record ResolvedPaths(string ObjectivesRoot, string ForgeRoot, string ObservationsPath);

    /// <param name="baseDirectory">Working/content root used when env vars are unset (typically repo root).</param>
    public static ResolvedPaths Resolve(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory is required.", nameof(baseDirectory));
        baseDirectory = Path.GetFullPath(baseDirectory);

        var obj = Environment.GetEnvironmentVariable("ASHLAR_OBJECTIVES_ROOT");
        var objectivesRoot = string.IsNullOrWhiteSpace(obj)
            ? Path.GetFullPath(Path.Combine(baseDirectory, ObjectiveStore.DefaultRelativePath))
            : Path.GetFullPath(obj.Trim());

        var forge = Environment.GetEnvironmentVariable("ASHLAR_FORGE_ROOT");
        var forgeRoot = string.IsNullOrWhiteSpace(forge)
            ? Path.GetFullPath(Path.Combine(baseDirectory, ".ashlar", "runtime-studio", "forge"))
            : Path.GetFullPath(forge.Trim());

        var obs = Environment.GetEnvironmentVariable("ASHLAR_OBSERVATIONS_PATH");
        var obsPath = string.IsNullOrWhiteSpace(obs)
            ? Path.GetFullPath(Path.Combine(baseDirectory, JsonlObservationStore.DefaultRelativePath))
            : Path.GetFullPath(obs.Trim());

        return new ResolvedPaths(objectivesRoot, forgeRoot, obsPath);
    }
}
