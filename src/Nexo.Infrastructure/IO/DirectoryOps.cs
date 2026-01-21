namespace Nexo.Infrastructure.IO;

/// <summary>
/// Infrastructure-only wrappers around raw directory APIs.
/// Keeps System.IO.Directory usage out of higher layers (CLI/Agents/Orchestration).
/// </summary>
public static class DirectoryOps
{
    public static void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public static void EnsureParentDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}

