using Nexo.Agents.AutonomousDev.Configuration;

namespace Nexo.Agents.AutonomousDev.Adapters;

/// <summary>
/// Platform-facing helper that infers project type from filesystem structure.
/// Kept in Adapters namespace so raw IO stays in platform-specific layer.
/// </summary>
internal static class ProjectTypeDetector
{
    public static ProjectType InferProjectType(string path)
    {
        if (Directory.Exists(Path.Combine(path, "Assets", "Scenes"))) return ProjectType.UnityGame;
        if (Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly).Any()) return ProjectType.DotNetApp;
        if (File.Exists(Path.Combine(path, "package.json"))) return ProjectType.ReactApp;
        return ProjectType.Generic;
    }
}

