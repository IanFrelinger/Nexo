namespace Ashlar.Infrastructure.IO;

/// <summary>
/// Infrastructure-only wrappers around raw directory APIs.
/// Keeps System.IO.Directory usage out of higher layers (CLI/Agents/Orchestration).
/// </summary>
public static class DirectoryOps
{
    /// <summary>Create directory.</summary>
    public static void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <summary>Ensures parent directory exists.</summary>
    public static void EnsureParentDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}

