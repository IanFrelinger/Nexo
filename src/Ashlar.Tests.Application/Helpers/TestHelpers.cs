namespace Ashlar.Tests.Application.Helpers;

/// <summary>
/// Helper utilities for tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a temporary directory with disposable cleanup. Use in using statements or with IDisposable.
    /// </summary>
    /// <param name="prefix">Prefix for the temp dir name (default: ashlar-test).</param>
    /// <returns>Path and a disposable that deletes the directory on Dispose.</returns>
    public static (string Path, IDisposable Cleanup) CreateTempDirectoryWithCleanup(string prefix = "ashlar-test")
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return (path, new TempDirCleanup(path));
    }

    private sealed class TempDirCleanup : IDisposable
    {
        private readonly string _path;
        public TempDirCleanup(string path) => _path = path;
        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                try { Directory.Delete(_path, recursive: true); } catch { /* ignore */ }
            }
        }
    }

    /// <summary>
    /// Creates a temporary directory for testing.
    /// </summary>
    public static DirectoryInfo CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        return Directory.CreateDirectory(tempPath);
    }

    /// <summary>
    /// Cleans up a temporary directory.
    /// </summary>
    public static void CleanupTempDirectory(DirectoryInfo dir)
    {
        if (dir.Exists)
        {
            try
            {
                Directory.Delete(dir.FullName, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Creates a temporary assembly file for testing.
    /// </summary>
    public static FileInfo CreateTempAssemblyFile(DirectoryInfo dir, string name = "test.dll")
    {
        var filePath = Path.Combine(dir.FullName, name);
        File.WriteAllText(filePath, "dummy assembly content");
        return new FileInfo(filePath);
    }

    /// <summary>
    /// Creates a temporary .cs file with an EmptyCatch pattern for improve tests.
    /// </summary>
    /// <param name="dir">Directory to create the file in.</param>
    /// <param name="name">Name of the file (default: EmptyCatch.cs).</param>
    /// <returns>Path to the created file.</returns>
    public static string CreateTempCsFileWithEmptyCatch(string dir, string name = "EmptyCatch.cs")
    {
        var path = Path.Combine(dir, name);
        var content = """
            using System;
            namespace Test;
            public class C
            {
                public void M()
                {
                    try { }
                    catch (Exception) { }
                }
            }
            """;
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a temporary test project file for testing.
    /// </summary>
    public static FileInfo CreateTempTestProjectFile(DirectoryInfo dir, string name = "TestProject.csproj")
    {
        var filePath = Path.Combine(dir.FullName, name);
        var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>";
        File.WriteAllText(filePath, content);
        return new FileInfo(filePath);
    }
}

